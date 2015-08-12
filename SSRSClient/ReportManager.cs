using Microsoft.SqlServer.ReportingServices2010;
using System.Net;
namespace SSRSClient
{
    public class ReportManager
    {
        private ReportingService2010 ReportingService { get; set; }

        public ReportManager()
        {
            ReportingService = new ReportingService2010();

        }

        public void InitializeSSRS()
        {
            ReportingService.Credentials = new NetworkCredential("A16007982-3", "Password1507", "EYDEV");
           
            var items = ReportingService.ListChildren("/", false);
        }
    }
}