using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Utils;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class EtcTest
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void Test_Etc()
        {
            bool testresults = true;
            try
            {
                NodeConfig config = new NodeConfig();
                Etc etcobj = new Etc(config);
                if (etcobj.GetAllUsers().Count() > 0)
                {
                    EtcUser user = etcobj.GetAllUsers().FirstOrDefault();
                    if (etcobj.GetPwanam(user.Name) == null)
                    {
                        testresults = false;
                    }
                }
            }
            catch
            {
                testresults = false;
            }
            Assert.AreEqual(true, testresults);
        }
    }
}
