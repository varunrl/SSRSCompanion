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

            var folders = items.OrderByDescending(x => x.ModifiedDate).Select(x => new CatalogObject { Name = x.Name + " - " + x.ModifiedDate.ToString("yyyy-MM-dd H:mm:ss"), Type = x.TypeName, Path = x.Path }).ToList();
            if (serverPath.Length > 1)
            {
                folders.Insert(0, new CatalogObject { Name = ".. Parent", Type = "Folder", Path = serverPath.LastIndexOf('/') == 0 ? "/" : serverPath.Remove(serverPath.LastIndexOf('/')) });
            }
            return folders;
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

        public bool createDatasource(string localFilePath, string serverFolderPath, bool overWrite, out string dataSourceName, out string dataSourcePath)
        {

            FileStream stream = File.OpenRead(localFilePath);

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(stream);

            dataSourceName = xmlDocument.DocumentElement.Attributes["Name"].Value;

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

            dataSourcePath = Addslash(serverFolderPath) + dataSourceName;

            return true;
        }

        public string DeployReport(string localPath, string serverPath,Dictionary<string,string> dataSources)
        {
            byte[] definition = null;
            Warning[] warnings = null;
            string retRes = String.Empty;

            
            // Read the file and put it into a byte array to pass to SRS
            definition = ReadFile(localPath, definition);
            

            // We are going to use the name of the rdl file as the name of our report
            string reportName = Path.GetFileNameWithoutExtension(localPath);


            // Now lets use this information to publish the report
           
                var catalog = ReportingService.CreateCatalogItem("Report", reportName, serverPath, true, definition, null,out warnings);

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




                DataSource[] dsarray = ReportingService.GetItemDataSources(Addslash(serverPath )+ reportName);

                foreach(var ds in dsarray)
                {
                    var destinationReference = ((Microsoft.SqlServer.ReportingServices2010.DataSourceReference)(ds.Item));

                    if (destinationReference != null)
                    {
                        if (destinationReference.Reference.LastIndexOf('/') != 0)
                        {
                            var dsname = destinationReference.Reference.Remove(destinationReference.Reference.LastIndexOf('/'));
                            if (dataSources.ContainsKey(dsname))
                            {
                                if (destinationReference.Reference != dataSources[ds.Name])
                                {
                                    DataSourceReference dsr = new DataSourceReference();
                                    dsr.Reference = dataSources[ds.Name];
                                    ds.Item = (DataSourceReference)dsr;
                                    retRes += String.Format("Setting DataSource to {0}\n", dsr.Reference);
                                }
                            }
                        }
                    }
                }

                ReportingService.SetItemDataSources(Addslash(serverPath) + reportName, dsarray);
                           

            return retRes;
        }

        private static byte[] ReadFile(string localPath, byte[] definition)
        {
            FileStream stream = File.OpenRead(localPath);
            definition = new byte[stream.Length];
            stream.Read(definition, 0, (int)(stream.Length));
            stream.Close();
            return definition;
        }


    }
}