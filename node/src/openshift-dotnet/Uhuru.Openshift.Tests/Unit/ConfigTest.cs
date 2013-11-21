using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.Openshift.Common;
using System.Collections.Generic;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class ConfigTest
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void Test_ReadConfig()
        {
            try
            {
                Config config = new Config(TestHelper.GetNodeConfigPath());
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }

        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_ReadValueConfig()
        {
            Config config = new Config(TestHelper.GetNodeConfigPath());

            string value = config.Get("CLOUD_DOMAIN");

            Assert.AreEqual("example.com", value);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_ReadDefaultConfig()
        {
            Config config = new Config(TestHelper.GetNodeConfigPath());

            string value = config.Get("not existant", "Value");

            Assert.AreEqual("Value", value);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GetGroupsConfig()
        {
            Config config = new Config(TestHelper.GetNodeConfigPath());

            Dictionary<string, string> value = config.GetGroup("Test Group", null);

            Assert.AreEqual(value["CONFIG_VALUE"], "testing");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GetConfigKeysConfig()
        {
            Config config = new Config(TestHelper.GetNodeConfigPath());
            
            string[] keys = config.GetParams();

            Assert.IsNotNull(keys.Length == 1);
        }


    }
}
