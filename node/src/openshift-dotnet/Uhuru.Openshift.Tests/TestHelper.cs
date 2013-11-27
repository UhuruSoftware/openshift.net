using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Utils;

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

        public static ApplicationContainer CreateAppContainer()
        {
            string applicationUuid = Guid.NewGuid().ToString("N");
            string containerUuid = applicationUuid;
            string userId = WindowsIdentity.GetCurrent().Name;
            string applicationName = "testApp";
            string containerName = applicationName;
            string namespaceName = "uhuru";
            object quotaBlocks = null;
            object quotaFiles = null;
            Hourglass hourglass = null;

            ApplicationContainer container = new ApplicationContainer(
                applicationUuid, containerUuid, userId,
                applicationName, containerName, namespaceName,
                quotaBlocks, quotaFiles, hourglass);

            return container;
        }
    }
}
