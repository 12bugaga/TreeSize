using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace TreeSize.ViewModel
{
    public class ViewItems : INotifyPropertyChanged
    {
        protected ObservableCollection<ModelItem> _modelItems;
        protected ConcurrentStack<int> concurrentStack;

        public ViewItems()
        {
            _modelItems = new ObservableCollection<ModelItem>();
            concurrentStack = new ConcurrentStack<int>();
        }

        public ObservableCollection<ModelItem> ViewItem
        {
            get { return _modelItems; }
        }

        public void Update()
        {
            OnPropertyChanged("ViewItems");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }


        public int Count()
        {
            return ViewItem.Count;
        }

        delegate void addItem(ModelItem item);
        public void AddItem(ModelItem item)
        {
            if (Application.Current.Dispatcher.CheckAccess())
                ViewItem.Add(item);
            else
            {
                addItem add = AddItem;
                Application.Current.Dispatcher.BeginInvoke(add, item);
            }
        }

        delegate void changeItem(ConcurrentStack<ViewItems> stack, ModelItem item);
        public void ChangeItem(ConcurrentStack<ViewItems> stack, ModelItem item)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                ViewItems number;
                stack.TryPop(out number);
                ViewItem.Remove(number.ViewItem[number.Count() - 1]);
                ViewItem.Add(item);
            }

            else
            {
                changeItem Change = ChangeItem;
                Application.Current.Dispatcher.BeginInvoke(Change, stack, item);
            }
        }
    }


    public class MyViewModel
    {

        protected ConcurrentStack<ViewItems> concurrentStack = new ConcurrentStack<ViewItems>();
        protected ConcurrentStack<int> concurrentStack1 = new ConcurrentStack<int>();

        ViewItems viewItems;
        public MyViewModel(ViewItems _viewItems)
        {
            viewItems = _viewItems;
        }

        public async void GetItemFromPathAsync(string path, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;
            await Task.Run(() => GetItemFromPath(path, token));
        }

        private void GetItemFromPath(string path, CancellationToken token)
        {
            ModelItem item;
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                DirectoryInfo[] diA = di.GetDirectories();
                FileInfo[] fi = di.GetFiles();
                double catalogSize;

                // Cycle for directory
                foreach (DirectoryInfo df in diA)
                {
                    if (token.IsCancellationRequested)
                        return;

                    catalogSize = 0;
                    item = (new ModelItem
                    {
                        Header = GetFileOrFolderName(df.FullName),
                        FullPath = df.FullName,
                        Status = "Waiting...",
                        Image = GetImage(df.FullName)
                    });
                    viewItems.AddItem(item);

                    item = (new ModelItem
                    {
                        Header = GetFileOrFolderName(df.FullName),
                        FullPath = df.FullName,
                        Status = "",
                        Image = GetImage(df.FullName),
                        ModelItems = GetChildrenItem(df.FullName, token),
                        VolumeMemory = GetVolMem(df.FullName, ref catalogSize),
                    });

                    concurrentStack.Push(viewItems);
                    viewItems.ChangeItem(concurrentStack, item);
                }

                // Cycle for all files
                foreach (FileInfo f in fi)
                {
                    if (token.IsCancellationRequested)
                        return;
                    item = new ModelItem
                    {
                        Header = GetFileOrFolderName(f.FullName),
                        FullPath = f.FullName,
                        Status = null,
                        Image = GetImage(f.FullName),
                        VolumeMemory = f.Length,
                    };
                    viewItems.AddItem(item);
                }
            }

            catch
            {
                item = new ModelItem
                {
                    Header = GetFileOrFolderName(path),
                    FullPath = path,
                    Image = GetImage(path),
                    Status = "Not available",
                    VolumeMemory = -1
                };
                viewItems.AddItem(item);
            }
        }

        private ObservableCollection<ModelItem> GetChildrenItem(string path, CancellationToken token)
        {
            ObservableCollection<ModelItem> childrenItems = new ObservableCollection<ModelItem>();
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                DirectoryInfo[] diA = di.GetDirectories();
                FileInfo[] fi = di.GetFiles();
                double catalogSize;

                // Cycle for directory
                foreach (DirectoryInfo df in diA)
                {
                    if (token.IsCancellationRequested)
                        break;
                    catalogSize = 0;
                    childrenItems.Add(new ModelItem
                    {
                        VolumeMemory = GetVolMem(df.FullName, ref catalogSize),
                        ModelItems = GetChildrenItem(df.FullName, token),
                        Status = null,
                        Header = GetFileOrFolderName(df.FullName),
                        FullPath = df.FullName,
                        Image = GetImage(df.FullName)
                    });

                }
                // Cycle for all files
                foreach (FileInfo f in fi)
                {
                    if (token.IsCancellationRequested)
                        break;
                    childrenItems.Add(new ModelItem
                    {
                        Header = GetFileOrFolderName(f.FullName),
                        FullPath = f.FullName,
                        Status = null,
                        Image = GetImage(f.FullName),
                        VolumeMemory = f.Length,
                    });
                }
            }

            catch
            {
                childrenItems.Add(new ModelItem
                {
                    Header = GetFileOrFolderName(path),
                    FullPath = path,
                    Image = GetImage(path),
                    Status = "Not available",
                    VolumeMemory = -1
                });
            }
            return childrenItems;
        }

        private double GetVolMem(string path, ref double catalogSize)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                DirectoryInfo[] diA = di.GetDirectories();
                FileInfo[] fi = di.GetFiles();
                // Cycle for all files
                foreach (FileInfo f in fi)
                {
                    // length Bytes
                    catalogSize += f.Length;
                }
                // Cycle for directory
                foreach (DirectoryInfo df in diA)
                {
                    GetVolMem(df.FullName, ref catalogSize);
                }
                //1ГБ = 1024 Байта * 1024 КБайта * 1024 МБайта
                return catalogSize;
            }
            catch
            {
                return 0;
            }
        }

        private string GetFileOrFolderName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return (string.Empty);

            string normalPath = path.Replace('/', '\\');
            int lastIndexName = normalPath.LastIndexOf('\\');

            if (lastIndexName <= 0)
                return path;

            //Take last value in string-path with separated \\
            return path.Substring(lastIndexName + 1);
        }

        private BitmapSource GetImage(string path)
        {
            //Get a full path
            if (path == null)
                return null;

            string image = "Images/file.png";
            string nameAFile = GetFileOrFolderName(path);

            //If name == null => drive
            if (string.IsNullOrEmpty(nameAFile))
                image = "Images/drive.png";
            else if (new FileInfo(path).Attributes.HasFlag(FileAttributes.Directory))
                image = "Images/folder-closed.png";

            var _image = new BitmapImage(new Uri($"pack://application:,,,/{image}"));
            _image.Freeze();

            return _image;
        }
    }
}
