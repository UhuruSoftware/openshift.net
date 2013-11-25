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
        public string CartridgeVendor { get { return "uhuru"; } }
        public string Name { get { return "openshift-origin-cartridge-dotnet"; } }
        public string CartridgeVersion { get { return "0.9"; } }
        public string ShortName { get { return "dotnet"; } }

        public static string BuildIdent(string vendor, string software, string softwareVersion, string cartridgeVersion)
        {
            vendor = vendor.ToLower();
            return string.Format("{0}:{1}:{2}:{3}", vendor, software, softwareVersion, cartridgeVersion);
        }
    }
}
