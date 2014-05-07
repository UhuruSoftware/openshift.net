using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime.Model;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class GearRegistryTest
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GearRegistry()
        {
            bool testresult = true;
            try
            {
                GearRegistry reg = new GearRegistry(TestHelper.CreateAppContainer());
                Dictionary<string, object> entryops = new Dictionary<string, object>();
                entryops["type"] = "testType";
                entryops["uuid"] = "test";
                entryops["namespace"] = "testnamespace";
                entryops["dns"] = "127.0.0.1";
                entryops["proxy_hostname"] = "broker.test.com";
                entryops["proxy_port"] = "65555";
                reg.Add(entryops); //Test fails in GearRegistry.cs line 116 ->it tries to access the gearRegistry key although the count is 0
                if (reg.Entries.Count > 0)
                {   
                    reg.Clear();
                }                
            }
            catch
            {
                testresult = false;
            }
            Assert.AreEqual(true, testresult);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GearRegistry_Entry()
        {
           bool testresult=false;
           Dictionary<string, object> entryops = new Dictionary<string, object>();                
           entryops["uuid"] = "test";
           entryops["namespace"] = "testnamespace";
           entryops["dns"] = "127.0.0.1";
           entryops["proxy_hostname"] = "broker.test.com";
           entryops["proxy_port"] = "65555";
           GearRegistry.Entry entry = new GearRegistry.Entry(entryops);
           string jsonEntry=entry.ToJson();
           string SshUrlEntry = entry.ToSshUrl();
           if (jsonEntry != string.Empty && SshUrlEntry != string.Empty)
           {
               testresult = true;
           }
           Assert.AreEqual(true, testresult);
        }
    }
}
