using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class ManifestTest
    {

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_CompareManifestVersions()
        {
            bool testresult = true;
            int result = Manifest.CompareVersions("5.0", "4.5");
            if (result != 1)
            {
                testresult = false;
            }
            result = Manifest.CompareVersions("5", "4.5");
            if (result != 1)
            {
                testresult = false;
            }
            result = Manifest.CompareVersions("4.5", "5");
            if (result != -1)
            {
                testresult = false;
            }
            result = Manifest.CompareVersions("5", "5");
            if (result != 0)
            {
                testresult = false;
            }
            Assert.AreEqual(true, testresult);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_BuildandParseIdent()
        {
            bool result = true;

            string ident = Manifest.BuildIdent("uhuru", "dotnet", "1", "4.5");
            if (ident.Where(o=>o == ':').Count()<3)
            {
                result = false;
            }
            string[] parsed = Manifest.ParseIdent(ident);
            if ((parsed[0] != "uhuru") || (parsed[1] != "dotnet") || (parsed[2]!="1")||(parsed[3]!="4.5"))
            {
                result = false;
            }
            try{
                result = false;
                string[] exceptionparse = Manifest.ParseIdent(ident.Replace(":", "-"));
            }
            catch (ArgumentException ex)
            {
                result = true;
            }            
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_ManifestProperties()
        {
            bool testresults = true;
            Manifest m = TestHelper.GetSampleManifest();
            if (m.InstallBuildRequired==false)
            {
                testresults = false;
            }
            if (m.Buildable == false)
            {
                testresults = false;
            }
            if (m.Dir == string.Empty)
            {
                testresults = false;
            }
            if (m.WebFramework == false)
            {
                testresults = false;
            }
            if (m.WebProxy == false)
            {
                testresults = false;
            }
            if (m.ToString() == string.Empty)
            {
                testresults = false;
            }            
            Assert.AreEqual(true, testresults);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_SortVersions()
        {
            bool sortedgood = true;

            IEnumerable<string> unsorted = new List<string>() {"5.0" , "4.5" ,"6" };
            List<string> sorted = Manifest.SortVersions(unsorted);
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                if (Manifest.CompareVersions(sorted[i], sorted[i + 1]) > 0)
                {
                    sortedgood = false;
                    break;
                }
            }

            Assert.AreEqual(true, sortedgood);
        }
    }
}
