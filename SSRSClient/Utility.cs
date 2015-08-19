using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSRSClient
{
    public static class  Utility
    {

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
