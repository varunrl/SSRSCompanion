using Microsoft.SqlServer.ReportingServices2010;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.IO;
using System.Xml;
using System.Threading.Tasks;
using System.Web.Services.Protocols;
using System.Diagnostics;
namespace SSRSClient
{
    public class ReportManager
    {
        private ReportingService2010 ReportingService { get; set; }

        public ReportManager(string url,string userName, string password)
        {
            ReportingService = new ReportingService2010();
            InitializeSSRS( url, userName, password);
        }

        private void InitializeSSRS(string url, string userName, string password)
        {
            url = Addslash(url);
            url = url + "ReportService2010.asmx";
            ReportingService.Url = url;
            ReportingService.Credentials = new NetworkCredential(userName,password);
           
            
        }

        private static string Addslash(string url)
        {
            if (url.LastIndexOf('/') != url.Length - 1)
            {
                url = url + "/";
            }
            return url;
        }

        public List<CatalogObject> GetFolders(string serverPath)
        {
            CatalogItem[] items = ReportingService.ListChildren (serverPath, false);
            
            var folders = items.OrderBy(x => x.Name).Select(x => new CatalogObject { Name = x.Name , Type = x.TypeName, Path = x.Path }).ToList();
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

            Parallel.ForEach(items.Where(x => x.TypeName == "Report" || x.TypeName == "DataSet"), currentfile =>
            {
                DataSource[] dsarray = ReportingService.GetItemDataSources(currentfile.Path);
                lock (locker)
                {
                    foreach(var ds in dsarray)
                    {
                        var type = ds.Item is DataSourceReference ? DSType.Reference : (ds.Item is DataSourceDefinition ? DSType.Definition : DSType.Invalid);
                        dsObjects.Add(new DataSourceObject
                            {
                                Name = currentfile.Name,
                                ModifiedOn = currentfile.ModifiedDate.ToString("yyyy-MM-dd H:mm:ss"),
                                Path = currentfile.Path,
                                Type = currentfile.TypeName,
                                DataSourceName = ds.Name,
                                DataSourceType = type,
                                ReferedDaraSource = (ds.Item as DataSourceReference) == null ? "" : (ds.Item as DataSourceReference).Reference,
                                IsSelected = true
                                
                            }
                            );
                    }
                    
                }
                
            });


            return dsObjects;
        }

        public List<CatalogObject> GetDataSources(string serverPath)
        {
            CatalogItem[] items = ReportingService.ListChildren(serverPath, false);

            var folders = items.Where(x => x.TypeName == "Folder" || x.TypeName == "DataSource").Select(x => new CatalogObject { Name = x.Name, Type = x.TypeName, Path = x.Path }).ToList();
            if (serverPath.Length > 1)
            {
                folders.Insert(0, new CatalogObject { Name = ".. Parent", Type = "Folder", Path = serverPath.LastIndexOf('/') == 0 ? "/" : serverPath.Remove(serverPath.LastIndexOf('/')) });
            }
            return folders;
        }

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
            byte[] definition = null;
            Warning[] warnings = null;
            string retRes = String.Empty;

            
            // Read the file and put it into a byte array to pass to SRS
            definition = ReadFile(localPath, definition);
            

            // We are going to use the name of the rdl file as the name of our report
            string reportName = Path.GetFileNameWithoutExtension(localPath);


            // Now lets use this information to publish the report

            var catalog = ReportingService.CreateCatalogItem(ReportManager.GetFileType(Path.GetExtension(localPath)), reportName, serverPath, true, definition, null, out warnings);

                if (warnings != null)
                {
                    retRes = String.Format("Report {0} created with warnings :\n", reportName);
                    foreach (Warning warning in warnings)
                    {
                        retRes += warning.Message + "\n";
                    }
                }
                else
                {
                    retRes = String.Format("Report {0} created successfully with no warnings\n", reportName);

                }       

            return retRes;
        }

        public void SetDataSources(List<DataSourceObject> list, string datasourcePath)
        {
            Parallel.ForEach(list.Where(x => x.IsSelected == true).GroupBy(x => new { Name = x.Name, Path = x.Path }), item => {
                
                DataSource[] dsarray = ReportingService.GetItemDataSources(item.Key.Path);
                foreach (var ds in dsarray)
                {
                    foreach(var gItem in item)
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


        private static byte[] ReadFile(string localPath, byte[] definition)
        {
            FileStream stream = File.OpenRead(localPath);
            definition = new byte[stream.Length];
            stream.Read(definition, 0, (int)(stream.Length));
            stream.Close();
            return definition;
        }



        public void DownloadReports(string localDirectory, string CurrentReportFolderPath)
        {
            CatalogItem[] items = ReportingService.ListChildren(CurrentReportFolderPath, false);

            Parallel.ForEach(items.Where(x => x.TypeName == "Report" || x.TypeName == "DataSource" || x.TypeName == "DataSet"), currentfile =>
            {
                DownloadItem(localDirectory, currentfile);
            });
          
        }


        private void DownloadItem(string localDirectory, CatalogItem item)
        {
            byte[] reportDefinition = null;
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

            reportDefinition = ReportingService.GetItemDefinition(item.Path);
            MemoryStream stream = new MemoryStream(reportDefinition);

            doc.Load(stream);
            doc.Save(Path.Combine(localDirectory + @"/",
                item.Name + GetExtensionfromType(item.TypeName)));
        }

        private string GetExtensionfromType(string Type)
        {
            switch(Type)
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


        public static string GetFileType(string p)
        {
            switch (System.IO.Path.GetExtension(p))
            {
                case ".rdl":
                    return "Report";
                case ".rds":
                    return "DataSource";
                case ".rsd":
                    return "DataSet";
                default:
                    return "NA";
            }
        }
    }
}