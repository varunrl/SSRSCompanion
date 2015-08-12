using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SSRSClient
{
    public class CatalogObject
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string Path { get; set; }

        public string PictureString
        {
            get { return "/Images/" + Type + ".png"; }
        }
    }
}
