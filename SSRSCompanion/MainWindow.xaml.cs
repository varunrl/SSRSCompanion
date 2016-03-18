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
        public IReportManager reportManager { get; set; }
        public string CurrentReportFolderPath { get; set; }
        public string CurrentDataSourceFolderPath { get; set; }

        public string SelectedDataSource { get; set; }
        public Dictionary<string,string> DataSources { get;set; }

        public int version { get; set; }

        public ProgressDialogController progressController { get;set; }
        public MainWindow()
        {
            InitializeComponent();
           
            CurrentReportFolderPath = "/";
            CurrentDataSourceFolderPath = "/";
            lblReportFolder.Content = CurrentReportFolderPath;

            DataSources = new Dictionary<string, string>();

            txtReportServer.Text = Settings.Default.ReportServer;
            txtUserName.Text = Settings.Default.UserName;
            txtLocaldirectory.Text = Settings.Default.LocalDirectory;
            LoadLocalfolder(txtLocaldirectory.Text);
            if(!String.IsNullOrWhiteSpace(Settings.Default.Password))
            {
                txtPassword.Password = Settings.Default.Password.DecryptString().ToInsecureString();
            }

        }

        private async Task showLoaderAsync(string message)
        {
            if (progressController == null || (progressController != null && progressController.IsOpen == false))
            {
                progressController = await this.ShowProgressAsync("Please wait...", message);
            }
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
            try
            {

                if (string.IsNullOrWhiteSpace(txtReportServer.Text) || string.IsNullOrWhiteSpace(txtUserName.Text) || string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    await this.ShowMessageAsync("Error", "Please input Report Server , User Name and Password");
                }
                else
                {
                    await showLoaderAsync("Loading Server Folders");
                    await InitialLoad(2010);
                    
                }
                await hideLoaderAsync();
                return;
            }
            catch (Exception exception)
            {
                

            }
            
            try
            {
                await InitialLoad(2005);
            }
            catch (Exception exception1)
            {
                LogException(exception1);
            }
            await hideLoaderAsync();
            
        }

        private async Task InitialLoad(int year)
        {
            if (reportManager == null || checkUserVariablesforchanges() || year != version)
            {
                version = year;
                if (version == 2010)
                {
                    reportManager = new ReportManager2010(txtReportServer.Text, txtUserName.Text, txtPassword.Password);
                }else
                {
                    reportManager = new ReportManager2005(txtReportServer.Text, txtUserName.Text, txtPassword.Password);
                }

                CurrentReportFolderPath = "/";
                CurrentDataSourceFolderPath = "/";
            }

            Task T1 = LoadFolderAndDetails();
            Task T2 = GetDatasourceFolderContentsAsync();
            await Task.WhenAll(T1, T2);
            btnDownload.IsEnabled = true;
            btnPublish.IsEnabled = true;
        }
        private void LogException(Exception exception)
        {
            MessageBox.Show(exception.Message + exception.StackTrace);
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
        private async Task GetDatasourceFolderContentsAsync()
        {
            //await showLoaderAsync("Loading Server Folders");

            var folders = await Task.Run(() =>
            {
                return reportManager.GetDataSources(CurrentDataSourceFolderPath);
            });

            dataSourceList.ItemsSource = folders;


        }


        private async Task GetReportFolderContentsAsync()
        {
            var folders = await Task.Run(() =>
            {
                return reportManager.GetFolders(CurrentReportFolderPath);
            });

            FolderList.ItemsSource = folders;

        }

        private async void FolderList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (FolderList.SelectedItem != null)
                {
                    var item = (CatalogObject)FolderList.SelectedItem;
                    if (item != null)
                    {
                        if (item.Type == "Folder")
                        {
                            CurrentReportFolderPath = item.Path;
                            lblReportFolder.Content = CurrentReportFolderPath;
                            await showLoaderAsync("Loading Server Folders");
                            await LoadFolderAndDetails();
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                LogException(ex);
            }
            await hideLoaderAsync();
        }

        private async Task LoadFolderAndDetails()
        {
            Task T1 = GetReportFolderContentsAsync();
            Task T2 = LoadDetails();
            await Task.WhenAll(T1, T2);
        }

        private async void dataSourceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (dataSourceList.SelectedItem != null)
                {
                    var item = (CatalogObject)dataSourceList.SelectedItem;
                    if (item != null)
                    {
                        if (item.Type == "Folder")
                        {
                            CurrentDataSourceFolderPath = item.Path;
                            await showLoaderAsync("Loading Server Folders");
                            await GetDatasourceFolderContentsAsync();
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                LogException(ex);
            }
            await hideLoaderAsync();
        }

        private void dataSourceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dataSourceList.SelectedItem != null)
                {
                    var item = (CatalogObject)dataSourceList.SelectedItem;
                    if (item != null)
                    {
                        lblDataSource.Content = item.Path;

                        if (item.Type == "DataSource" )
                        {
                            SelectedDataSource = item.Path;
                            btnSetDataSources.IsEnabled = true;
                        }else
                        {
                            SelectedDataSource = "";
                            btnSetDataSources.IsEnabled = false;
                        }

                    }
                }
            }
            catch (Exception ex)
            {

                LogException(ex);
            }


        }

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

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                if (!string.IsNullOrWhiteSpace(txtLocaldirectory.Text))
                {
                    dialog.SelectedPath = txtLocaldirectory.Text;
                }

                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    txtLocaldirectory.Text = dialog.SelectedPath;
                    LoadLocalfolder(txtLocaldirectory.Text.ToString());
                    Settings.Default.LocalDirectory = txtLocaldirectory.Text.Trim();
                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {

                LogException(ex);
            }
           
        }

        private void LoadLocalfolder(string path)
        {
            if (!String.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                DirectoryInfo d = new DirectoryInfo(path);
                var files = d.GetFiles().Select(x => new CatalogObject { Name = x.Name, Path = x.FullName, Type = Utility.GetFileType(x.FullName) });
                LocalFolderList.ItemsSource = files;
            }else
            {
                 txtLocaldirectory.Text = "";
            }
        }

       

        private async void btnPublish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageDialogResult result = await this.ShowMessageAsync("Confirmation", "Are you sure to upload reports to server folder " + this.CurrentReportFolderPath, MessageDialogStyle.AffirmativeAndNegative);
                if (result == MessageDialogResult.Affirmative)
                {
                    await PublishAsync();
                }
            }
            catch (Exception ex)
            {

                LogException(ex);
            }
           
        }

        private async Task PublishAsync()
        {
            await showLoaderAsync("Publishing Reports");
            var localDirectory = txtLocaldirectory.Text;
            await Task.Run(() =>
            {
                PublishToReportServer(localDirectory);
                
            });
            await LoadFolderAndDetails();
            await hideLoaderAsync();
          
        }

        private void PublishToReportServer(string localDirectory)
        {
            Parallel.ForEach(Directory.GetFiles(localDirectory), new ParallelOptions { MaxDegreeOfParallelism = 4 }, currentfile =>
            {
                if (System.IO.Path.GetExtension(currentfile) == ".rds")
                {
                    reportManager.createDatasource(currentfile, CurrentReportFolderPath, false);

                }else if(System.IO.Path.GetExtension(currentfile) == ".rsd" || System.IO.Path.GetExtension(currentfile) == ".rdl" )
                {
                    reportManager.DeployReport(currentfile, CurrentReportFolderPath);
                }
            });
                      
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                 MessageDialogResult result =  await this.ShowMessageAsync("Confirmation", "You are going to download reports to  " +
                     txtLocaldirectory.Text + " from  " + CurrentReportFolderPath, MessageDialogStyle.AffirmativeAndNegative);

                 if (result == MessageDialogResult.Affirmative)
                 {
                     await showLoaderAsync("Downloading Reports");

                     var localDirectory = txtLocaldirectory.Text;

                     await Task.Run(() =>
                     {
                         DownLoadtoLocalDirectory(localDirectory);

                     });
                     LoadLocalfolder(localDirectory);
                    
                 }

                
            }
            catch (Exception ex)
            {

                LogException(ex);
            }
            
            await hideLoaderAsync();
        }

        private void DownLoadtoLocalDirectory(string localDirectory)
        {
            reportManager.DownloadReports(localDirectory, CurrentReportFolderPath);
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadLocalfolder(txtLocaldirectory.Text);
            }
            catch (Exception ex)
            {

                LogException(ex);
            }
        }


        private async Task LoadDetails()
        {

             var folders = await Task.Run(() =>
            {
                return reportManager.GetFoldersWithDetails(CurrentReportFolderPath);
            });


            DataSourceObjectGrid.ItemsSource = folders;
            
        }

        private async void btnSetDataSources_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(SelectedDataSource))
                {
                    await this.ShowMessageAsync("Error", "Please select datasource");
                }
                else if (DataSourceObjectGrid.ItemsSource as List<DataSourceObject> == null)
                {
                    await this.ShowMessageAsync("Error", "Please Load details");
                }
                else if (DataSourceObjectGrid.ItemsSource as List<DataSourceObject> != null && (DataSourceObjectGrid.ItemsSource as List<DataSourceObject>).Where(x => x.IsSelected == true).Count() == 0)
                {
                    await this.ShowMessageAsync("Error", "Please select Reports/DataSets from the list");
                }
                else
                {
                    MessageDialogResult result =  await this.ShowMessageAsync("Confirmation", "You are going to set datasource " + SelectedDataSource + " on " + (DataSourceObjectGrid.ItemsSource as List<DataSourceObject>).Where(x => x.IsSelected == true).Count() + " Objects",MessageDialogStyle.AffirmativeAndNegative);

                    if (result == MessageDialogResult.Affirmative)
                    {
                        await showLoaderAsync("Setting DataSources");
                        List<DataSourceObject> test = DataSourceObjectGrid.ItemsSource as List<DataSourceObject>;
                        var datasource = SelectedDataSource;

                        await Task.Run(() =>
                        {
                            reportManager.SetDataSources(test, datasource);

                        });
                        await LoadDetails();

                        
                    }
                }
            }
            catch (Exception ex)
            {
                
                LogException(ex);
            }
            
             await hideLoaderAsync();
            
        }
    }
}
