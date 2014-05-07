using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime.Utils;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class EnvironTest
    {
        [TestInitialize]
        public void Init()
        {
            if (Directory.Exists("TestGearDir"))
            {
                DirectoryUtil.EmptyDirectory("TestGearDir");
                Directory.Delete("TestGearDir");
            }
            Directory.CreateDirectory("TestGearDir");
            Directory.CreateDirectory(@"TestGearDir\.env");
            Directory.CreateDirectory(@"TestGearDir\.env\USER_VARS");
            File.WriteAllText(@"TestGearDir\.env\TESTHOME", "testcONTENT");
            File.WriteAllText(@"TestGearDir\.env\TESTENVFILE", "testcONTENT");
            File.WriteAllText(@"TestGearDir\.env\USER_VARS\TESTUSERENVFILE", "TestuserVAR");
            File.WriteAllText(@"TestGearDir\.env\USER_VARS\ERBTESTUSERFILE.erb", "Test Empty ERB File");            
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_Environ()
        {
            string gearDir="TestGearDir";
            Dictionary<string, string> result = Environ.ForGear(gearDir);
            DirectoryUtil.EmptyDirectory(gearDir);
            Directory.Delete(gearDir);
            Assert.IsNotNull(result);
        }
    }
}
