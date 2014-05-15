using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class UtilitiesTest
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void Test_DirectoryandFileUtil()
        {
            bool result = true;
            try
            {
                if (!Directory.Exists("Test1"))
                {
                    Directory.CreateDirectory("Test1");
                    Directory.CreateDirectory(@"Test1\Testing");
                }
                File.WriteAllText(@"Test1\test1.txt", "Testing");
                DirectoryUtil.DirectoryCopy("Test1", "Test2", true);
                DirectoryUtil.EmptyDirectory("Test2");
                if (Directory.GetFiles("Test2").Length == 0)
                {
                    Directory.Delete("Test2");
                }                
                DirectoryUtil.CreateSymLink(@"Test1\TestLink", @"Test1\test1.txt", DirectoryUtil.SymbolicLink.File);
                string location=FileUtil.GetSymlinkTargetLocation(@"Test1\TestLink");
                if (location == string.Empty)
                {
                    result = false;
                }
                DirectoryUtil.EmptyDirectory("Test1");
                Directory.Delete("Test1");
            }
            catch
            {
                result = false;
            }
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_RubyHash()
        {
            bool test = true;
            try
            {
                Dictionary<string, object> ctorTestO = new Dictionary<string, object>();
                ctorTestO.Add("testkey", new List<int>() { 1, 2, 3, 4 });
                Dictionary<string, string> ctorTest = new Dictionary<string, string>();
                ctorTest.Add("test", "testValue");
                RubyHash hash = new RubyHash(ctorTestO);
                RubyHash hash2 = new RubyHash(ctorTest);
                RubyHash mergedHash = hash.Merge(hash2);
                mergedHash = hash.Merge(ctorTest);
                mergedHash = hash2.Merge(ctorTestO);
            }
            catch
            {
                test = false;
            }
            Assert.AreEqual(true, test);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_RubyCompatibility()
        {
            int epochValue = RubyCompatibility.DateTimeToEpochSeconds(new DateTime(1970, 1, 1));
            Assert.AreEqual(0, epochValue);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_Arguments()
        {
            string[] args = new string[] { "uuid:1234","-helo:world","-withField:test:mark", "-magic", "-test","s s","s"};
            Arguments result = new Arguments(args);            
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_ProcessExtensions()
        {
            bool testresult = true;
            try
            {                
                System.Diagnostics.Process process = new System.Diagnostics.Process();                
                process.StartInfo.FileName = "cmd";
                process.Start();
                try
                {
                    ProcessExtensions.KillProcessAndChildren(process);
                }
                catch 
                {
                    if (!process.HasExited)
                    {
                        throw new Exception("Test Failure");
                    }                    
                }                
                process.Start();
                try
                {
                    ProcessExtensions.KillProcessAndChildren(process.Id);
                }
                catch
                {
                    if (!process.HasExited)
                    {
                        throw new Exception("Test Failure");
                    }                    
                }
                string powershell = ProcessExtensions.Get64BitPowershell();
                if (powershell == string.Empty)
                {
                    testresult = false;
                }
            }
            catch
            {
                testresult = false;
            }
            Assert.AreEqual(true, testresult);
        }
    }
}
