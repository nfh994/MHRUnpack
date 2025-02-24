using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MHRUnpack.FileStructs;
using MHRUnpack.Utils;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WPFLocalizeExtension.Engine;

namespace MHRUnpack.ViewModels
{
    public enum Lang
    {
        zh,
        en
    }
    public partial class MainViewModel : ObservableObject
    {
        #region 标题栏
        [ObservableProperty]
        string _Title = Application.ResourceAssembly.GetName().Name + Application.ResourceAssembly.GetName().Version.ToString();//标题
        //string _Title = "MHRUnpack";
        [ObservableProperty]
        bool _Topmost = false;//窗口置顶
        [ObservableProperty]
        Lang _Language = Lang.zh;//语言
        partial void OnLanguageChanged(Lang value)
        {
            string lang = value.ToString();
            var info = new CultureInfo(lang);
            Thread.CurrentThread.CurrentCulture = info;
            Thread.CurrentThread.CurrentUICulture = info;
            LocalizeDictionary.Instance.Culture = info;

            Properties.Settings.Default.语言 = lang;
            Properties.Settings.Default.Save();
        }
        #endregion

        #region 属性
        [ObservableProperty]
        bool _ShowUnk = false;//显示未知文件
        partial void OnShowUnkChanged(bool value)
        {
            Properties.Settings.Default.显示未知 = value;
            Properties.Settings.Default.Save();
        }
        [ObservableProperty]
        bool _MultiThread;//启用多线程
        partial void OnMultiThreadChanged(bool value)
        {
            Properties.Settings.Default.多线程 = value;
            Properties.Settings.Default.Save();
        }
        [ObservableProperty]
        int _ThreadCount;//线程最大数量
        partial void OnThreadCountChanged(int value)
        {
            Properties.Settings.Default.线程数 = value;
            Properties.Settings.Default.Save();
        }
        [ObservableProperty]
        string _OutputPath;
        partial void OnOutputPathChanged(string value)
        {
            Properties.Settings.Default.输出目录 = value;
            Properties.Settings.Default.Save();
        }
        #region List
        [ObservableProperty]
        ObservableCollection<string> _ListFiles = new ObservableCollection<string>();
        [ObservableProperty]
        string _SelectedList;
        [ObservableProperty]
        bool _NoLoadingList = true;
        partial void OnSelectedListChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Task.Run(() =>
                {
                    NoLoadingList = false;
                    HashManager.LoadHashList(value);
                    NoLoadingList = true;
                });
            }
        }
        [RelayCommand]
        public void FlushListFiles()
        {
            if (Directory.Exists(HashManager.ListPath))
            {
                ListFiles.Clear();
                var files = Directory.GetFiles(HashManager.ListPath);
                foreach (var file in files)
                {
                    if (file.EndsWith(".list"))
                    {
                        ListFiles.Add(Path.GetFileName(file));
                    }
                }
            }
            SelectedList = null;
            HashManager.LoadedCache.Clear();
        }
        #endregion

        #region PakPathList
        [ObservableProperty]
        ObservableCollection<string> _PakFiles = new ObservableCollection<string>();
        [ObservableProperty]
        string _SelectedPak;
        [RelayCommand]
        public void SelectPak()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Pak files (*.pak)|*.pak";
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == true)
            {
                AddPak(dialog.FileNames);
            }
        }
        private void AddPak(string[] files)
        {
            foreach (var file in files)
            {
                if (!File.Exists(file))
                {
                    continue;
                }
                if (!file.EndsWith(".pak"))
                {
                    continue;
                }
                if (!PakFiles.Contains(file))
                {
                    PakFiles.Add(file);
                }
            }
        }
        [RelayCommand]
        public void MoveUp()
        {
            if (SelectedPak != null)
            {
                int index = PakFiles.IndexOf(SelectedPak);
                if (index > 0)
                {
                    PakFiles.Move(index, index - 1);
                }
            }
        }
        [RelayCommand]
        public void MoveDown()
        {
            if (SelectedPak != null)
            {
                int index = PakFiles.IndexOf(SelectedPak);
                if (index < PakFiles.Count - 1)
                {
                    PakFiles.Move(index, index + 1);
                }
            }
        }
        [RelayCommand]
        public void DelPak()
        {
            if (SelectedPak != null)
            {
                int index = PakFiles.IndexOf(SelectedPak);
                PakFiles.Remove(SelectedPak);
            }
        }
        [RelayCommand]
        public async Task ParsePak()
        {
            if (PakFiles.Count == 0)
            {
                MessageBox.Show("点击+或拖入添加Pak文件");
                return;
            }
            if (string.IsNullOrWhiteSpace(SelectedList))
            {
                MessageBox.Show("选择一个list文件");
                return;
            }
            ConcurrentDictionary<UInt64, PakEntry> entrys = new();

            foreach (var file in PakFiles)
            {
                await Task.Run(() =>
                {
                    using Pak pak = new Pak(file);
                    if (!pak.ReadEntry(out var lpTable))
                    {
                        return;
                    }
                    if (lpTable == null || lpTable.Length == 0)
                    {
                        return;
                    }
                    int ThreadCount = 1;
                    if (Properties.Settings.Default.多线程)
                    {
                        ThreadCount = Properties.Settings.Default.线程数;
                    }
                    int entrysPerThread = pak.Header.dwTotalFiles / ThreadCount;
                    Task[] tasks = new Task[ThreadCount];
                    for (int i = 0; i < ThreadCount; i++)
                    {
                        int start = i * entrysPerThread;
                        int end = (i == ThreadCount - 1) ? pak.Header.dwTotalFiles : (start + entrysPerThread);
                        tasks[i] = Task.Run(() =>
                        {
                            using BinaryReader reader = new(new MemoryStream(lpTable));
                            reader.BaseStream.Position = start * pak.Header.dwEntrySize;
                            for (int j = start; j < end; j++)
                            {
                                PakEntry entry;
                                if (pak.Header.bMajorVersion == 2)
                                {
                                    entry = new PakEntryV2();
                                }
                                else
                                {
                                    entry = new PakEntryV4();
                                }
                                entry.Read(reader);
                                if (!entrys.ContainsKey(entry.Hash))
                                {
                                    entry.From = file;
                                    entrys.TryAdd(entry.Hash, entry);
                                }
                            }
                        });
                    }
                    Task.WaitAll(tasks);
                });
            }
            ParseTree(entrys);
        }
        #endregion

        #endregion

        #region Tree
        [ObservableProperty]
        ObservableCollection<TreeModel> _Tree = new();
        public void ParseTree(ConcurrentDictionary<UInt64, PakEntry> entrys)
        {
            TreeModel root = new("root");
            TreeModel unknown = new("unknown");
            unknown.SetParent(root);

            int ThreadCount = 1;
            if (Properties.Settings.Default.多线程)
            {
                ThreadCount = Properties.Settings.Default.线程数;
            }
            int per = entrys.Count / ThreadCount;
            Task[] tasks = new Task[ThreadCount];
            var keys = entrys.Keys.ToList();
            for (int i = 0; i < ThreadCount; i++)
            {
                int start = i * per;
                int end = (i == ThreadCount - 1) ? entrys.Count : (start + per);
                tasks[i] = Task.Run(() =>
                {
                    for (int j = start; j < end; j++)
                    {
                        var hash = keys[j];
                        var entry = entrys[hash];
                        if (HashManager.HashList.ContainsKey(hash))
                        {
                            if (HashManager.HashList.TryGetValue(hash, out entry.Path))
                            {
                                var names = entry.Path.Split("/");
                                TreeModel parent = root;
                                for (int k = 0; k < names.Length; k++)
                                {
                                    var name = names[k];
                                    //已存在节点
                                    if (parent.SafeChildren.TryGetValue(name, out var first))
                                    {
                                        parent = first;
                                        continue;
                                    }
                                    bool isFile = k == names.Length - 1;
                                    TreeModel model = new(name, isFile);
                                    if (!model.SetParent(parent))
                                    {
                                        k--;
                                        continue;
                                    }
                                    if (isFile)
                                    {
                                        model.Entry = entry;
                                        //if (entry.wCompressionType == Compression.DEFLATE)
                                        //{
                                        //    Debug.WriteLine(entry.Path);
                                        //}
                                        //if (entry.wEncryptionType != Encryption.None)
                                        //{
                                        //    Debug.WriteLine(entry.Path);
                                        //}
                                    }
                                    parent = model;
                                }
                                continue;
                            }
                            else
                            {
                                MessageBox.Show("读取已有hash失败");
                                continue;
                            }
                        }
                        var unk_name = hash.ToString("X16");
                        entry.Path = $"unknown/{unk_name}";
                        TreeModel unk_model = new(unk_name, true);
                        if (!unk_model.SetParent(unknown))
                        {
                            j--;
                            continue;
                        }
                        unk_model.Entry = entry;
                    }
                });
            }
            Task.WaitAll(tasks);
            root.Sort();
            Tree.Clear();
            Tree.Add(root);
        }
        #endregion

        [RelayCommand]
        public void SelectOutput()
        {
            var dialog = new OpenFolderDialog();
            if ((bool)dialog.ShowDialog())
            {
                OutputPath = dialog.FolderName;
            }
        }
        [RelayCommand]
        public async Task Unpack()
        {
            if (TreeModel.SelectedItem.IsEmpty)
            {
                MessageBox.Show("未选择文件");
                return;
            }
            if (string.IsNullOrWhiteSpace(OutputPath))
            {
                MessageBox.Show("选择输出目录");
                return;
            }

            await Task.Run(() =>
            {
                int ThreadCount = 1;
                if (Properties.Settings.Default.多线程)
                {
                    ThreadCount = Properties.Settings.Default.线程数;
                }
                int total = TreeModel.SelectedItem.Count;
                if (total < ThreadCount)
                {
                    ThreadCount = 1;
                }
                int per = total / ThreadCount;
                Task[] tasks = new Task[ThreadCount];
                var keys = TreeModel.SelectedItem.Keys.ToList();
                for (int i = 0; i < ThreadCount; i++)
                {
                    int start = i * per;
                    int end = (i == ThreadCount - 1) ? total : (start + per);
                    tasks[i] = Task.Run(() =>
                    {
                        ConcurrentDictionary<string, BinaryReader> pakReader = new();
                        for (int j = start; j < end; j++)
                        {
                            var entry = keys[j];
                            var model = TreeModel.SelectedItem[entry];
                            if (model.Entry == null)
                            {
                                continue;
                            }
                            if (!pakReader.TryGetValue(model.Entry.From, out var reader))
                            {
                                reader = new(File.OpenRead(model.Entry.From));
                                if (!pakReader.TryAdd(model.Entry.From, reader))
                                {
                                    reader.Dispose();
                                    reader = pakReader[model.Entry.From];
                                }
                            }
                            reader.BaseStream.Position = model.Entry.dwOffset;
                            byte[] data = reader.ReadBytes((int)model.Entry.dwCompressedSize);
                            var path = Path.Combine(OutputPath, model.Entry.Path);
                            if (!Directory.Exists(Path.GetDirectoryName(path)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(path));
                            }
                            if (model.Entry.wCompressionType == Compression.NONE)
                            {
                                File.WriteAllBytes(path, data);
                            }
                            else if (model.Entry.wCompressionType == Compression.DEFLATE || model.Entry.wCompressionType == Compression.ZSTD)
                            {
                                var lpDstBuffer = new Byte[] { };

                                if (model.Entry.wEncryptionType != Encryption.None && model.Entry.wEncryptionType <= Encryption.Type_Invalid)
                                {
                                    data = ResourceCipher.iDecryptResource(data);
                                }

                                switch (model.Entry.wCompressionType)
                                {
                                    case Compression.DEFLATE: lpDstBuffer = CompressionUtil.iDecompress(data); break;
                                    case Compression.ZSTD: lpDstBuffer = CompressionUtil.ZSTDDecompress(data); break;
                                }

                                File.WriteAllBytes(path, lpDstBuffer);
                            }
                        }
                        foreach (var item in pakReader.Values)
                        {
                            item.Dispose();
                        }
                    });
                }
                Task.WaitAll(tasks);
            });
        }
        public MainViewModel()
        {
            Language = Properties.Settings.Default.语言 == "en" ? Lang.en : Lang.zh;
            MultiThread = Properties.Settings.Default.多线程;
            ThreadCount = Properties.Settings.Default.线程数;
            ShowUnk = Properties.Settings.Default.显示未知;
            OutputPath = Properties.Settings.Default.输出目录;
            FlushListFiles();
        }
        [RelayCommand]
        public void Test()
        {
            Tree.First().IsSelected = null;
            //if (PakFiles.Count == 0)
            //{
            //    MessageBox.Show("请添加Pak文件");
            //    return;
            //}
            //using Pak pak = new Pak();
            //pak.Read(PakFiles[0]);
            //byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            //using BinaryReader r1 = new(new MemoryStream(bytes));
            //using BinaryReader r2 = new(new MemoryStream(bytes));
            //r1.BaseStream.Position = 3;
            //r2.BaseStream.Position = 5;
        }

        #region Event
        [RelayCommand]
        private void Closing(System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("您确定要退出吗？", "确认", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
        [RelayCommand]
        private void GridSplitter_DragDelta(System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var gridSplitter = e.OriginalSource as System.Windows.Controls.GridSplitter;
            if (gridSplitter != null)
            {
                // 获取 GridSplitter 所在 Grid
                var grid = gridSplitter.Parent as Grid;

                if (grid != null)
                {
                    var col = grid.ColumnDefinitions[2];
                    double min = 250;
                    double max = grid.ActualWidth / 2;
                    if (max < min)
                    {
                        max = min;
                    }

                    // 获取当前拖动的位置
                    double newW = col.ActualWidth - e.HorizontalChange;

                    // 限制拖动范围
                    if (newW < min)
                    {
                        newW = min;
                    }
                    else if (newW > max)
                    {
                        newW = max;
                    }
                    col.Width = new GridLength(newW);
                }
            }
        }
        [RelayCommand]
        private void Drop(DragEventArgs e)
        {
            // 获取拖入的文件路径
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddPak(files);
        }
        #endregion

    }
}
