using MahApps.Metro.Controls;
using SSRSClient;
using System;
using System.Collections.Generic;
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
        public MainWindow()
        {
            InitializeComponent();
           
            CurrentReportFolderPath = "/";
            CurrentDataSourceFolderPath = "/";
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

        private void btnGetFolders_Click(object sender, RoutedEventArgs e)
        {
            reportManager = new ReportManager(txtReportServer.Text, txtUserName.Text, txtPassword.Text);
            GetDatasourceFolderContents();
            GetReportFolderContents();
        }
        private void GetDatasourceFolderContents()
        {
            var folders = reportManager.GetDataSources(CurrentDataSourceFolderPath);
            dataSourceList.ItemsSource = folders;
        }


        private void GetReportFolderContents()
        {
            var folders = reportManager.GetFolders(CurrentReportFolderPath);
            FolderList.ItemsSource = folders;
        }

        private void FolderList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
                        GetReportFolderContents();
                    }
                }
            }
        }

        private void dataSourceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dataSourceList.SelectedItem != null)
            {
                var item = (CatalogObject)dataSourceList.SelectedItem;
                if (item != null)
                {
                    if (item.Type == "Folder")
                    {
                        CurrentDataSourceFolderPath = item.Path;
                        
                        GetDatasourceFolderContents();
                    }
                }
            }
        }

        private void dataSourceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataSourceList.SelectedItem != null)
            {
                var item = (CatalogObject)dataSourceList.SelectedItem;
                if (item != null)
                {
                    lblDataSource.Content = item.Path;
                }
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
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if(result == System.Windows.Forms.DialogResult.OK)
            {
                txtLocaldirectory.Text = dialog.SelectedPath;
            }
           
        }
    }
}
