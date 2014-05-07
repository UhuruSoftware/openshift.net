using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Common.Utils;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class NetworkTest
    {
        private int port = 0;
                
        [TestInitialize]
        public void GetPort()
        {
            port = Network.GetAvailablePort(1000);
        }

        [TestMethod]
        [TestCategory("Unit")]
        //Test port generation with default values
        public void Test_GetPredictablePort(){            
            Assert.AreEqual(10001, port);
        }      

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_NetworkFirewall()
        {
            try
            {
                Network.OpenFirewallPort(port.ToString(), "Test");
                Network.CloseFirewallPort(port.ToString());
            }
            catch
            {
                Assert.Fail();
            }
            Assert.AreEqual(1, 1);
        }
    }
}
