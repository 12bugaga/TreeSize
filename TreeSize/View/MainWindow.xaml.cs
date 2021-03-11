using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using TreeSize.ViewModel;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading;

namespace TreeSize
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        CancellationTokenSource cts;
        public void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog openFileDlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            string choiceFolder;
            MyViewModel _model = new MyViewModel();
            if (openFileDlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                choiceFolder = openFileDlg.FileName;
                ChoiceFolder.Text = choiceFolder;
                DataContext = _model;

                cts = new CancellationTokenSource();
                _model.GetItemFromPathAsync(choiceFolder, cts.Token);
                CancelButton.IsEnabled = true;
            }
        }

        public void CancelButton_Click (object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            CancelButton.IsEnabled = false;
        }
    }
}
