using System.Windows;
using TreeSize.ViewModel;
using Microsoft.WindowsAPICodePack.Dialogs;
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
            ViewItems _model = new ViewItems();

            MyViewModel start = new MyViewModel(_model);
            if (openFileDlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                choiceFolder = openFileDlg.FileName;
                ChoiceFolder.Text = choiceFolder;
                DataContext = _model;

                cts = new CancellationTokenSource();
                start.GetItemFromPathAsync(choiceFolder, cts.Token);
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
