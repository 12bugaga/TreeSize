using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TreeSize;

namespace TreeSize.ViewModel
{
    public class MyViewModel : INotifyPropertyChanged
    {
        protected ObservableCollection<ModelItem> _modelItems;

        public MyViewModel()
        {
            _modelItems = new ObservableCollection<ModelItem>();
        }

        public ObservableCollection<ModelItem> ViewItems
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


        delegate void addItem(ModelItem item);
        private void AddItem (ModelItem item)
        {
            if (Application.Current.Dispatcher.CheckAccess())
                ViewItems.Add(item);
            else
            { 
                addItem add = AddItem;
                Application.Current.Dispatcher.BeginInvoke(add, item);
            }
        }
        
        delegate void changeItem(int number, ModelItem item);
        private void ChangeItem(int numberItem, ModelItem item)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                ViewItems.Remove(ViewItems[numberItem]);
                ViewItems.Add(item);
            }

            else
            {
                changeItem Change = ChangeItem;
                Application.Current.Dispatcher.BeginInvoke(Change, numberItem, item);
            }
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
                    AddItem(item);

                    item = (new ModelItem
                    {
                        Header = GetFileOrFolderName(df.FullName),
                        FullPath = df.FullName,
                        Status = "",
                        Image = GetImage(df.FullName),
                        ModelItems = GetChildrenItem(df.FullName, token),
                        VolumeMemory = GetVolMem(df.FullName, ref catalogSize),
                    });
                    ChangeItem(ViewItems.Count-1, item);
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
                    AddItem(item);
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
                AddItem(item);
            }
        }

        private ObservableCollection<ModelItem> GetChildrenItem(string path, CancellationToken token)
        {
            ObservableCollection<ModelItem> childrenItems= new ObservableCollection<ModelItem>();
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

        private double  GetVolMem(string path, ref double catalogSize)
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
