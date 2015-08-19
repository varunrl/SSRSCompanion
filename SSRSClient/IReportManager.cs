using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSRSClient
{
    public interface IReportManager
    {
        bool createDatasource(string localFilePath, string serverFolderPath, bool overWrite);
        string DeployReport(string localPath, string serverPath);
        void DownloadReports(string localDirectory, string CurrentReportFolderPath);
        List<CatalogObject> GetDataSources(string serverPath);

        List<CatalogObject> GetFolders(string serverPath);
        List<DataSourceObject> GetFoldersWithDetails(string serverPath);

        void SetDataSources(List<DataSourceObject> list, string datasourcePath);
    }
}
