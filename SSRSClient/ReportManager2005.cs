using Microsoft.SqlServer.ReportingServices2005;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Services.Protocols;
using System.Xml;

namespace SSRSClient
{
    public class ReportManager2005: IReportManager
    {
        public ReportManager2005(string url, string userName, string password)
        {
            ReportingService = new ReportingService2005();
            InitializeSSRS(url, userName, password);
        }

        private ReportingService2005 ReportingService { get; set; }
        

        public bool createDatasource(string localFilePath, string serverFolderPath, bool overWrite)
        {
            FileStream stream = File.OpenRead(localFilePath);

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(stream);

            var dataSourceName = Path.GetFileNameWithoutExtension(localFilePath);

            string connectionString = xmlDocument.SelectSingleNode("//ConnectString").InnerText;

            DataSourceDefinition definition = new DataSourceDefinition();
            definition.CredentialRetrieval = CredentialRetrievalEnum.Integrated;
            definition.ConnectString = connectionString;
            definition.Enabled = true;
            definition.EnabledSpecified = true;
            definition.Extension = "SQL";
            definition.ImpersonateUserSpecified = false;

            definition.WindowsCredentials = true;

            try
            {
                ReportingService.CreateDataSource(dataSourceName, serverFolderPath, overWrite, definition, null);
            }
            catch (SoapException exception)
            {
                if (exception.Message.Contains("Microsoft.ReportingServices.Diagnostics.Utilities.ItemAlreadyExistsException"))
                {
                    Debug.Print(exception.Message);
                }
                else
                {
                    throw exception;
                }
            }

            return true;
        }

        public string DeployReport(string localPath, string serverPath)
        {
            if (Utility.GetFileType(Path.GetExtension(localPath)) == "Report")
            {
                byte[] definition = null;
                string retRes = String.Empty;

                // Read the file and put it into a byte array to pass to SRS
                definition = ReadFile(localPath, definition);

                // We are going to use the name of the rdl file as the name of our report
                string reportName = Path.GetFileNameWithoutExtension(localPath);

                // Now lets use this information to publish the report

                var catalog = ReportingService.CreateReport(reportName, serverPath, true, definition, null);

                
                return retRes;
            }
            return "";
        }

        public void DownloadReports(string localDirectory, string CurrentReportFolderPath)
        {
            CatalogItem[] items = ReportingService.ListChildren(CurrentReportFolderPath, false);

            Parallel.ForEach(items.Where(x => x.Type == ItemTypeEnum.Report || x.Type == ItemTypeEnum.DataSource), currentfile =>
            {
                DownloadItem(localDirectory, currentfile);
            });
        }

        public List<CatalogObject> GetDataSources(string serverPath)
        {
            CatalogItem[] items = ReportingService.ListChildren(serverPath, false);

            var folders = items.Where(x => x.Type == ItemTypeEnum.Folder || x.Type == ItemTypeEnum.DataSource).Select(x => new CatalogObject { Name = x.Name, Type = Enum.GetName(typeof(ItemTypeEnum), x.Type), Path = x.Path }).ToList();
            if (serverPath.Length > 1)
            {
                folders.Insert(0, new CatalogObject { Name = ".. Parent", Type = "Folder", Path = serverPath.LastIndexOf('/') == 0 ? "/" : serverPath.Remove(serverPath.LastIndexOf('/')) });
            }
            return folders;
        }

        public List<CatalogObject> GetFolders(string serverPath)
        {
            CatalogItem[] items = ReportingService.ListChildren(serverPath, false);

            var folders = items.OrderBy(x => x.Name).Select(x => new CatalogObject { Name = x.Name, Type = Enum.GetName(typeof(ItemTypeEnum), x.Type), Path = x.Path }).ToList();
            if (serverPath.Length > 1)
            {
                folders.Insert(0, new CatalogObject { Name = ".. Parent", Type = "Folder", Path = serverPath.LastIndexOf('/') == 0 ? "/" : serverPath.Remove(serverPath.LastIndexOf('/')) });
            }
            return folders;
        }

        public List<DataSourceObject> GetFoldersWithDetails(string serverPath)
        {
            CatalogItem[] items = ReportingService.ListChildren(serverPath, false);

            object locker = new object();

            var dsObjects = new List<DataSourceObject>();

            Parallel.ForEach(items.Where(x => x.Type == ItemTypeEnum.Report), currentfile =>
            {
                DataSource[] dsarray = ReportingService.GetItemDataSources(currentfile.Path);
                lock (locker)
                {
                    foreach (var ds in dsarray)
                    {
                        var type = ds.Item is DataSourceReference ? DSType.Reference : (ds.Item is DataSourceDefinition ? DSType.Definition : DSType.Invalid);
                        dsObjects.Add(new DataSourceObject
                            {
                                Name = currentfile.Name,
                                ModifiedOn = currentfile.ModifiedDate.ToString("yyyy-MM-dd H:mm:ss"),
                                Path = currentfile.Path,
                                Type = Enum.GetName(typeof(ItemTypeEnum), currentfile.Type),
                                DataSourceName = ds.Name,
                                DataSourceType = type,
                                ReferedDataSource = (ds.Item as DataSourceReference) == null ? "" : (ds.Item as DataSourceReference).Reference,
                                IsSelected = true
                            }
                            );
                    }
                }
            });

            return dsObjects;
        }

        public void SetDataSources(List<DataSourceObject> list, string datasourcePath)
        {
            Parallel.ForEach(list.Where(x => x.IsSelected == true).GroupBy(x => new { Name = x.Name, Path = x.Path }), item =>
            {
                DataSource[] dsarray = ReportingService.GetItemDataSources(item.Key.Path);
                foreach (var ds in dsarray)
                {
                    foreach (var gItem in item)
                    {
                        if (ds.Name == gItem.DataSourceName)
                        {
                            DataSourceReference dsr = new DataSourceReference();
                            dsr.Reference = datasourcePath;
                            ds.Item = (DataSourceReference)dsr;
                        }
                    }
                }

                ReportingService.SetItemDataSources(item.Key.Path, dsarray);
            });
        }

        private static string Addslash(string url)
        {
            if (url.LastIndexOf('/') != url.Length - 1)
            {
                url = url + "/";
            }
            return url;
        }

        private static byte[] ReadFile(string localPath, byte[] definition)
        {
            FileStream stream = File.OpenRead(localPath);
            definition = new byte[stream.Length];
            stream.Read(definition, 0, (int)(stream.Length));
            stream.Close();
            return definition;
        }

        private void DownloadItem(string localDirectory, CatalogItem item)
        {
            

            if(item.Type == ItemTypeEnum.Report)
            {
                byte[] reportDefinition = null;
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                reportDefinition = ReportingService.GetReportDefinition(item.Path);
                MemoryStream stream = new MemoryStream(reportDefinition);

                doc.Load(stream);
                doc.Save(Path.Combine(localDirectory + @"/",
                    item.Name + GetExtensionfromType(Enum.GetName(typeof(ItemTypeEnum), item.Type))));
            }
            
            
            
        }

        private string GetExtensionfromType(string Type)
        {
            switch (Type)
            {
                case "Report":
                    return ".rdl";

                case "DataSource":
                    return ".rds";

                case "DataSet":
                    return ".rsd";

                default:
                    return ".unknown";
            }
        }

        private void InitializeSSRS(string url, string userName, string password)
        {
            url = Addslash(url);
            url = url + "ReportService2005.asmx";
            ReportingService.Url = url;
            ReportingService.Credentials = new NetworkCredential(userName, password);
        }
    }
}