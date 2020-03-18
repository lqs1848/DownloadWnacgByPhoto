using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace wnacg
{
    class Comic
    {
        public string Id { get; set; }
        public string Title {get;set;}
        public Dictionary<int, string> Contents { get; set; }
        public string Cover { get; set; }
        public Comic() {
            Contents = new Dictionary<int, string>();
        }

    }
}
