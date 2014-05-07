using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime.Utils;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class SshdTest
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void Test_Sshd()
        {
            bool testResult = true;
            try
            {
                if (Directory.Exists("TestHomeDir"))
                {
                    Directory.Delete("TestHomeDir", true);
                }
                Sshd.ConfigureSshd(@"c:\openshift\cygwin\installation", "testUser", "Administrator", "TestHomeDir", string.Empty);
                Sshd.AddKey(@"c:\openshift\cygwin\installation", "testUser", "testkey");
                Sshd.RemoveKey(@"c:\openshift\cygwin\installation", "testUser", "testkey");
                Sshd.RemoveUser(@"c:\openshift\cygwin\installation", "testUser", "Administrator", "TestHomeDir", string.Empty);
            }
            catch
            {
                testResult = false;
            }
            Assert.IsTrue(testResult);
        }
    }
}
