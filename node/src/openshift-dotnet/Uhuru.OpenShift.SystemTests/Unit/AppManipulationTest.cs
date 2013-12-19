using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uhuru.OpenShift.SystemTests.Unit
{

    [TestClass]
    public class AppManipulationTest
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void SetupRHC()
        {
            Assert.IsTrue(TestHelper.SetupRHC());
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void CreateDomain()
        {
            string domainName = "test2";

            string output = TestHelper.RunRHC(String.Format("create-domain {0}", domainName), false);

            Assert.IsTrue(output.Contains(String.Format("Creating domain '{0}' ... done", domainName)));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AppCreate()
        {
            
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AppDeleteSucces()
        {
            string appName = "dotnet3";

            //after the app was created, get app gear to check if the gear folder is deleted from window node location

            string output = TestHelper.RunRHC(String.Format("app-delete {0} --confirm", appName), false);

            Assert.IsTrue(output.Contains(String.Format("Deleting application '{0}' ... deleted", appName)));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AppDeleteFailed()
        {
            string appName = "dotnet3";

            string output = TestHelper.RunRHC(String.Format("app-delete {0} --confirm", appName), false);

            Assert.IsTrue(output.Contains(String.Format("Application '{0}' not found.", appName)));
        }
    }
}
