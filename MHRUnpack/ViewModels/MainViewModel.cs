using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
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
        //string _Title = Application.ResourceAssembly.GetName().Name + Application.ResourceAssembly.GetName().Version.ToString();//标题
        string _Title = "MHRUnpack";
        [ObservableProperty]
        bool _Topmost = false;//窗口置顶
        [ObservableProperty]
        Lang _Language = Lang.zh;//语言
        partial void OnLanguageChanged(Lang value)
        {
            string lang = value.ToString();
            var t = Thread.CurrentThread.CurrentCulture;
            var c = CultureInfo.CurrentCulture;
            var info = new CultureInfo(lang);
            Thread.CurrentThread.CurrentCulture = info;
            Thread.CurrentThread.CurrentUICulture = info;
            LocalizeDictionary.Instance.Culture = info;
            I18NExtension.Culture = info;
        }
        #endregion
        #region 属性
        [ObservableProperty]
        bool _ShowUnk = false;//显示未知文件
        [ObservableProperty]
        bool _MultiThread = true;//启用多线程
        [ObservableProperty]
        int _ThreadCount = 5;//线程最大数量
        [ObservableProperty]
        string _OutputPath;
        [RelayCommand]
        public void SelectOutput()
        {
            var dialog = new OpenFolderDialog();
            if ((bool)dialog.ShowDialog())
            {
                OutputPath = dialog.FolderName;
            }
        }
        #region List
        [ObservableProperty]
        ObservableCollection<string> _ListFiles = new ObservableCollection<string>();
        [ObservableProperty]
        string _SelectedList;
        [RelayCommand]
        public void FlushListFiles()
        {
            var path = "./Lists";
            if (Directory.Exists(path))
            {
                ListFiles.Clear();
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    if (file.EndsWith(".list"))
                    {
                        ListFiles.Add(Path.GetFileName(file));
                    }
                }
            }
            SelectedList = null;
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
                foreach (var file in dialog.FileNames)
                {
                    if(!PakFiles.Contains(file))
                    {
                        PakFiles.Add(file);
                    }
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
        #endregion

        #endregion


        public MainViewModel()
        {
            FlushListFiles();
        }
        #region Event
        [RelayCommand]
        public void Closing(System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("您确定要退出吗？", "确认", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
        [RelayCommand]
        public void GridSplitter_DragDelta(System.Windows.Controls.Primitives.DragDeltaEventArgs e)
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
                    if(max < min)
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

        #endregion

    }
}
