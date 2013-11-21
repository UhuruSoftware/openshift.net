using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.Openshift.Common;
using System.IO;
using System.Linq;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class ParseConfigTest
    {
       
        [TestMethod]
        [TestCategory("Unit")]
        public void Test_LoadConfigFile()
        {
            ParseConfig config;
            try
            {
                config = new ParseConfig(TestHelper.GetNodeConfigPath());
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }          
            
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GetConfigValue()
        {
            ParseConfig config = new ParseConfig(TestHelper.GetNodeConfigPath());

            string value = config.GetValue("CLOUD_DOMAIN");

            Assert.AreEqual("example.com", value);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GetSectionConfigValue()
        {
            ParseConfig config = new ParseConfig(TestHelper.GetNodeConfigPath());

            string value = config.GetValue("CONFIG_VALUE", "Test Group");

            Assert.AreEqual("testing", value);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GetDefaultConfigValue()
        {
            ParseConfig config = new ParseConfig(TestHelper.GetNodeConfigPath());

            string value = config.GetValue("not existant", "Test Group", "Value");

            Assert.AreEqual("Value", value);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GetDefaultConfigValueNoGroup()
        {
            ParseConfig config = new ParseConfig(TestHelper.GetNodeConfigPath());

            string value = config.GetValue("not existant", "not existant", "Value");

            Assert.AreEqual("Value", value);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GetConfigKeys()
        {
            ParseConfig config = new ParseConfig(TestHelper.GetNodeConfigPath());

            string[] keys = config.GetKeys("");
            
            Assert.IsNotNull(keys.Select(k => k == "CONFIG_VALUE").Count() > 0);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GetCategoryConfigKeys()
        {
            ParseConfig config = new ParseConfig(TestHelper.GetNodeConfigPath());

            string[] keys = config.GetKeys("Test Group");

            Assert.IsNotNull(keys.Count() == 1);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GetConfigSectionsConfig()
        {
            ParseConfig config = new ParseConfig(TestHelper.GetNodeConfigPath());

            string[] sections = config.GetSections();

            Assert.IsNotNull(sections.Count() == 1);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GetConfigComents()
        {
            ParseConfig config = new ParseConfig(TestHelper.GetNodeConfigPath());

            string value = config.GetValue("MOTD_FILE");

            Assert.AreEqual(value, "");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_SaveParserConfigFile()
        {
            ParseConfig config = new ParseConfig(TestHelper.GetNodeConfigPath());
            string newFilePath = System.IO.Path.GetTempFileName();

            try
            {
                config.Save(newFilePath);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
            finally
            {
                File.Delete(newFilePath);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_AddKeyParserConfig()
        {
            ParseConfig config = new ParseConfig(TestHelper.GetNodeConfigPath());
            string newFilePath = System.IO.Path.GetTempFileName();

            config.WriteValue("NEWKEY", "NEWVALUE");
            config.Save(newFilePath);
            ParseConfig newConfig = new ParseConfig(newFilePath);

            string value = newConfig.GetValue("NEWKEY");

            Assert.AreEqual("NEWVALUE", value);
            File.Delete(newFilePath);
            
        }
        
        

    }
}
