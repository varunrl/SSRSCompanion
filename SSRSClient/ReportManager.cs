using Microsoft.SqlServer.ReportingServices2010;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.IO;
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
            if (url.IndexOf('/') == url.Length)
            {
                url = url + "/";
            }
            url = url + "ReportService2010.asmx";
            ReportingService.Url = url;
            ReportingService.Credentials = new NetworkCredential(userName,password);
           
            
        }

        public List<CatalogObject> GetFolders(string serverPath)
        {
            CatalogItem[] items = ReportingService.ListChildren (serverPath, false);

            var folders = items.Select(x => new CatalogObject { Name = x.Name, Type = x.TypeName, Path = x.Path }).ToList();
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

        public string createDatasource(string localPath, string serverPath)
        {
            //byte[] definition = null;
            //Warning[] warnings = null;
            string retRes = String.Empty;

            
            //// Read the file and put it into a byte array to pass to SRS
            //definition = ReadFile(localPath, definition);

            // // We are going to use the name of the rdl file as the name of our report
            //string dataSourceName = Path.GetFileNameWithoutExtension(localPath);


            //var catalog = ReportingService.CreateCatalogItem("DataSource", dataSourceName, serverPath, true, definition, null, out warnings);

            //if (warnings != null)
            //{
            //    retRes = String.Format("DataSource {0} created with warnings :\n", dataSourceName);
            //    foreach (Warning warning in warnings)
            //    {
            //        retRes += warning.Message + "\n";
            //    }
            //}
            //else
            //{
            //    retRes = String.Format("DataSource {0} created successfully with no warnings\n", dataSourceName);

            //}


            DataSourceDefinition definition = new DataSourceDefinition();
            definition.CredentialRetrieval = CredentialRetrievalEnum.Integrated;
            definition.ConnectString = "Data Source=comp_name\\abcd;Initial Catalog=abcd_ODS";
            definition.Enabled = true;
            definition.EnabledSpecified = true;
            definition.Extension = "SQL";
            definition.ImpersonateUserSpecified = false;

            definition.WindowsCredentials = true;

            
                ReportingService.CreateDataSource("Test", serverPath, true, definition, null);
                
           




            return retRes;
        }

        public string DeployReport(string localPath, string serverPath,string dataSourceName)
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

                //set the datasource
                DataSourceReference dsr = new DataSourceReference();
                dsr.Reference = serverPath + "/" + dataSourceName;


                DataSource[] dsarray = ReportingService.GetItemDataSources(serverPath + "/" + reportName);
                DataSource ds = new DataSource();

                ds = dsarray[0];
                ds.Item = (DataSourceReference)dsr;

                ReportingService.SetItemDataSources(serverPath + "/" + reportName, dsarray);
                retRes += String.Format("Data source succesfully set to {0}\n", dsr.Reference);

            

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