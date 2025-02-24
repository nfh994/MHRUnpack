using CommunityToolkit.Mvvm.ComponentModel;
using MHRUnpack.FileStructs;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows;

namespace MHRUnpack.ViewModels
{
    public partial class TreeModel : ObservableObject
    {
        public static ConcurrentDictionary<string, TreeModel> SelectedItem = new();
        [ObservableProperty]
        string _Icon = "\uE8B7";
        [ObservableProperty]
        string _Name;
        [ObservableProperty]
        bool _IsFile;
        [ObservableProperty]
        bool _IsExpanded;
        [ObservableProperty]
        bool? _IsSelected = false;
        partial void OnIsSelectedChanged(bool? value)
        {
            if (value == null)
            {
                return;
            }

            foreach (var child in Children)
            {
                if (child.Visibility == Visibility.Visible)
                {
                    child.IsSelected = value;
                }
            }
            if (IsFile)
            {
                if (value == true)
                {
                    SelectedItem.TryAdd(Entry.Path, this);
                }
                else
                {
                    SelectedItem.TryRemove(Entry.Path, out _);
                }
            }
            UpdateParentSelection(Parent);
        }

        private void UpdateParentSelection(TreeModel parent)
        {
            if (parent == null)
            {
                return;
            }
            bool allSelected = true;
            bool allUnselected = true;

            foreach (var item in parent.Children)
            {
                if (item.IsSelected == true)
                {
                    allUnselected = false;
                }
                else if (item.IsSelected == false)
                {
                    allSelected = false;
                }
                else
                {
                    allSelected = false;
                    allUnselected = false;
                }

                if (!allSelected && !allUnselected)
                {
                    break;
                }
            }

            if (allSelected)
            {
                parent.IsSelected = true;
            }
            else if (allUnselected)
            {
                parent.IsSelected = false;
            }
            else
            {
                parent.IsSelected = null;
            }
            UpdateParentSelection(parent.Parent);
        }
        [ObservableProperty]
        int _FileCount = 0;
        [ObservableProperty]
        int _SelectedCount = 0;
        [ObservableProperty]
        double _Size = 0;
        [ObservableProperty]
        string _StrSize;
        [ObservableProperty]
        Visibility _Visibility = Visibility.Visible;
        [ObservableProperty]
        ObservableCollection<TreeModel> _Children = new ObservableCollection<TreeModel>();
        //public ConcurrentBag<TreeModel> SafeChildren = new ConcurrentBag<TreeModel>();
        public ConcurrentDictionary<string, TreeModel> SafeChildren = new();
        public PakEntry Entry { get; set; }
        public TreeModel Parent { get; set; }
        public bool SetParent(TreeModel parent)
        {
            Parent = parent;
            return parent.SafeChildren.TryAdd(Name, this);
        }
        public TreeModel(string name, bool isFile = false)
        {
            Name = name;
            IsFile = isFile;
            if (IsFile)
            {
                Icon = "\uE7C3";
            }
        }
        public string ParseSize(double size)
        {
            string[] strings = ["B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];
            int index = 0;
            while (size >= 1024)
            {
                size /= 1024;
                index++;
            }
            return $"{size:0.##}{strings[index]}";
        }
        public void Sort()
        {
            int fileCount = 0;
            double size = 0;
            foreach (var (name, model) in SafeChildren)
            {
                model.Sort();
                fileCount += model.FileCount;
                size += model.Size;
            }
            if (SafeChildren.IsEmpty)
            {
                if (IsFile)
                {
                    FileCount = 1;
                    Size = Entry.dwDecompressedSize;
                }
                else
                {
                    FileCount = 0;
                }
                StrSize = ParseSize(Size);
                return;
            }
            if (SafeChildren.Count == 1)
            {
                var model = SafeChildren.First().Value;
                if (!model.IsFile)
                {
                    Name = $"{Name}/{model.Name}";
                    SafeChildren = model.SafeChildren;
                    Sort();
                    return;
                }
            }
            FileCount = fileCount;
            Size = size;
            StrSize = ParseSize(Size);
            // 排序: 先按 IsFile == false 排在前面, 然后按 Name 排序
            Children = new ObservableCollection<TreeModel>(SafeChildren.Values.OrderBy(x => x.IsFile).ThenBy(x => x.Name));
        }
        public override string ToString()
        {
            string str = Name;
            if (IsFile)
            {
                str = $"{Name} (Size:{StrSize} From:{Entry.From})";
                //str = $"{Name} (压缩后:{ParseSize(Entry.dwCompressedSize)} Size:{StrSize} 压缩:{Entry.wCompressionType} T:{Entry.wEncryptionType} From:{Entry.From})";
                //str = $"{Name} (压缩:{Entry.wCompressionType} From:{Entry.wEncryptionType})";
            }
            else
            {
                str = $"{Name} (File:{FileCount} Size:{StrSize})";// Selected:{SelectedCount}
            }
            return str;

        }
    }
}
