using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Tests
{
    public class TestHelper
    {
        const string nodeConfigFile = "Assets/node_test.conf";

        public static string GetNodeConfigPath()
        {
            string path = Path.GetFullPath(nodeConfigFile);
            return path;
        }
       

    }
}
