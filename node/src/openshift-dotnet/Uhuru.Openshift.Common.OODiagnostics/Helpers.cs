using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Common.OODiagnostics
{
    public static class Helpers
    {
        static string nodeConfigPath = @"C:\openshift\node.conf";
        static string mcollectiveSrvConfigPath = @"C:\openshift\mcollective\etc\server.cfg";
        public static Config GetNodeConfig()
        {
            Config config = new Config(nodeConfigPath);
            return config;
        }

        public static Config GetMcollectiveSrvConfig()
        {
            Config config = new Config(mcollectiveSrvConfigPath);
            return config;
        }
    }
}
