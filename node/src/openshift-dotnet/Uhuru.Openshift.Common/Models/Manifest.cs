using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime
{
    public class Manifest
    {
        public string PrivateIpName { get; set; }
        public string PrivatePortName { get; set; }
        public int PrivatePort { get; set; }
        public string PublicPortName { get; set; }
        public string WebsocketPortName { get; set; }
        public int WebsocketPort { get; set; }        
        public string RepositoryPath { get { return @"I:\_code\openshift.net\cartridges\openshift-origin-cartridge-dotnet"; } }
        public string Dir { get { return "dotnet"; } }
    }
}
