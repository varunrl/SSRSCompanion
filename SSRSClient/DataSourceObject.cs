using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SSRSClient
{
    public class DataSourceObject
    {
        public bool IsSelected { get; set; }

        public string Name { get; set; }

        public string ModifiedOn { get; set; }

        public string Type { get; set; }

        public string Path { get; set; }

        public string DataSourceName { get; set; }

        public DSType DataSourceType { get; set; }

        public string ReferedDataSource { get; set; }

        
    }


    public enum DSType
    {
        Definition = 1,
        Reference = 2,
        Invalid = 3
    }
}
