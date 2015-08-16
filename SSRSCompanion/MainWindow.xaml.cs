using MahApps.Metro.Controls;
using SSRSClient;
using SSRSCompanion.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls.Dialogs;
using System.ComponentModel;

namespace SSRSCompanion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow :  MetroWindow

    {
        public ReportManager reportManager { get; set; }
        public string CurrentReportFolderPath { get; set; }
        public string CurrentDataSourceFolderPath { get; set; }
        public Dictionary<string,string> DataSources { get;set; }

        public ProgressDialogController progressController { get;set; }
        public MainWindow()
        {
            InitializeComponent();
           
            CurrentReportFolderPath = "/";
            CurrentDataSourceFolderPath = "/";

            DataSources = new Dictionary<string, string>();

            txtReportServer.Text = Settings.Default.ReportServer;
            txtUserName.Text = Settings.Default.UserName;
            txtLocaldirectory.Text = Settings.Default.LocalDirectory;
            LoadLocalfolder(txtLocaldirectory.Text);
            if(!String.IsNullOrWhiteSpace(Settings.Default.Password))
            {
                txtPassword.Password = Settings.Default.Password.DecryptString().ToInsecureString();
            }

            try
            {
                //ReportManager reportManager = new ReportManager();
                //var folders = reportManager.GetFolders();
                //reportManager.createDatasource(@"C:\Users\varun.robinson\Desktop\Projects\SSRS deployment\datasource\TASRMTDataSource111.rds", "/GDN_TASRMT");

                //reportManager.DeployReport(@"C:\Users\varun.robinson\Desktop\Projects\SSRS deployment\datasource\DailyTeamUtilization.rdl", "/GDN_TASRMT", "test");
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.StackTrace);
                
            }

        }

        private async Task showLoaderAsync(string message)
        {
            progressController = await this.ShowProgressAsync("Please wait...", message);
        }

        private async Task hideLoaderAsync()
        {
            if (progressController != null && progressController.IsOpen == true)
            {
                await progressController.CloseAsync();
            }
        }
        private async void btnGetFolders_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtReportServer.Text) || string.IsNullOrWhiteSpace(txtUserName.Text) || string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                await this.ShowMessageAsync("Error", "Please input Report Server , User Name and Password");
            }
            else
            {

               
                //GetDatasourceFolderContents();
                await GetReportFolderContentsAsync();
            }
        }

        private bool checkUserVariablesforchanges()
        {
            bool changed = false;
            if(txtReportServer.Text.Trim() != Settings.Default.ReportServer.Trim())
            {
                changed = true;
                Settings.Default.ReportServer = txtReportServer.Text.Trim();
            }
            if (txtUserName.Text.Trim() != Settings.Default.UserName.Trim())
            {
                changed = true;
                Settings.Default.UserName = txtUserName.Text.Trim();
            }

            if (txtPassword.Password.Trim().ToSecureString().EncryptString() != Settings.Default.Password.Trim())
            {
                changed = true;
                Settings.Default.Password = txtPassword.Password.Trim().ToSecureString().EncryptString();
            }
            Settings.Default.Save();

            return changed;

        }
        //private void GetDatasourceFolderContents()
        //{
        //    var folders = reportManager.GetDataSources(CurrentDataSourceFolderPath);
        //    dataSourceList.ItemsSource = folders;
        //}


        private async Task GetReportFolderContentsAsync()
        {
            await showLoaderAsync("Loading Server Folders");
            if (reportManager == null || checkUserVariablesforchanges())
            {
                reportManager = new ReportManager(txtReportServer.Text, txtUserName.Text, txtPassword.Password);
            }
            if (reportManager != null)
            {
                System.Windows.Threading.Dispatcher pdDispatcher = this.Dispatcher;
                BackgroundWorker worker;
                worker = new BackgroundWorker();
                worker.WorkerSupportsCancellation = true;

                worker.DoWork += delegate(object s, DoWorkEventArgs args)
                {
                    var folders = reportManager.GetFolders(CurrentReportFolderPath);
                    pdDispatcher.BeginInvoke((Action)(() => { FolderList.ItemsSource = folders; }));
                };
                worker.RunWorkerCompleted += async delegate(object s, RunWorkerCompletedEventArgs args)
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message + args.Error.StackTrace);
                    }
                    
                    await hideLoaderAsync();
                    
                };
                worker.RunWorkerAsync();
            }else
            {
                MessageBox.Show("Input Details are not correct");
            }
        }

        private async void FolderList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FolderList.SelectedItem != null)
            {
                var item = (CatalogObject)FolderList.SelectedItem;
                if (item != null)
                {
                    if (item.Type == "Folder")
                    {
                        CurrentReportFolderPath = item.Path ;
                        lblReportFolder.Content = CurrentReportFolderPath;
                        await GetReportFolderContentsAsync();
                    }
                }
            }
        }

        //private void dataSourceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    if (dataSourceList.SelectedItem != null)
        //    {
        //        var item = (CatalogObject)dataSourceList.SelectedItem;
        //        if (item != null)
        //        {
        //            if (item.Type == "Folder")
        //            {
        //                CurrentDataSourceFolderPath = item.Path;
                        
        //                GetDatasourceFolderContents();
        //            }
        //        }
        //    }
        //}

        //private void dataSourceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (dataSourceList.SelectedItem != null)
        //    {
        //        var item = (CatalogObject)dataSourceList.SelectedItem;
        //        if (item != null)
        //        {
        //            lblDataSource.Content = item.Path;
        //        }
        //    }
        

        //}

        private void FolderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FolderList.SelectedItem != null)
            {
                var item = (CatalogObject)FolderList.SelectedItem;
                if (item != null)
                {
                    //to do
                }
            }
        }

        private async void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if(!string.IsNullOrWhiteSpace(txtLocaldirectory.Text))
            {
                dialog.SelectedPath = txtLocaldirectory.Text;
            }
            
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if(result == System.Windows.Forms.DialogResult.OK)
            {
                txtLocaldirectory.Text = dialog.SelectedPath;
                LoadLocalfolder(txtLocaldirectory.Text.ToString());
               
            }
           
        }

        private void LoadLocalfolder(string path)
        {
            if (!String.IsNullOrWhiteSpace(path))
            {
                DirectoryInfo d = new DirectoryInfo(path);
                var files = d.GetFiles().Select(x => new CatalogObject { Name = x.Name, Path = x.FullName, Type = GetFileType(x.FullName) });
                LocalFolderList.ItemsSource = files;
            }
        }

        private string GetFileType(string p)
        {
            switch(System.IO.Path.GetExtension(p))
            {
                case ".rdl":
                    return "Report";
                case ".rds":
                    return "DataSource";
                default:
                    return "NA";
            }
        }

        private async void btnPublish_Click(object sender, RoutedEventArgs e)
        {
            await PublishAsync();
           
        }

        private async Task PublishAsync()
        {
            await showLoaderAsync("Publishing Reports");
            //System.Windows.Threading.Dispatcher pdDispatcher = this.Dispatcher;
            BackgroundWorker worker;
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            var localDirectory = txtLocaldirectory.Text;
            worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                PublishToReportServer(localDirectory);
                //pdDispatcher.BeginInvoke((Action)(() => { Publish(); }));
            };
            worker.RunWorkerCompleted += async delegate(object s, RunWorkerCompletedEventArgs args)
            {
                await hideLoaderAsync();
                await GetReportFolderContentsAsync();
                if (args.Error != null)
                {
                    MessageBox.Show(args.Error.Message + args.Error.StackTrace);
                }
                else
                {
                    await this.ShowMessageAsync("Success", "Reports published sucessfully");
                }
            };
            worker.RunWorkerAsync();
        }

        private void PublishToReportServer(string localDirectory)
        {
            DataSources.Clear();
            foreach (string fileName in Directory.GetFiles(localDirectory, "*.rds"))
            {
                string dataSourceName, dataSourcePath;
                reportManager.createDatasource(fileName, CurrentReportFolderPath, false, out dataSourceName, out dataSourcePath);
                DataSources.Add(dataSourceName, dataSourcePath);
            }
            Parallel.ForEach(Directory.GetFiles(localDirectory, "*.rdl"), currentfile =>
            {

                reportManager.DeployReport(currentfile, CurrentReportFolderPath, DataSources);
            });

           
        }
    }
}
