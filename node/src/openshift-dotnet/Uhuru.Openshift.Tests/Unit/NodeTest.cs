using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.Openshift.Runtime;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.QualityTools.Testing.Fakes.Shims;
using System.IO;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Utils;
using Uhuru.Openshift.Runtime.Fakes;
using System.Diagnostics;
using Uhuru.Prison.Fakes;
using Uhuru.Openshift.Common.Utils;
using Uhuru.Openshift.Runtime.Model.ApplicationContainerExt;
using Uhuru.Openshift.Common.JsonHelper;
using System.Collections.Generic;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Tests.Unit
{  

    [TestClass]
    public class NodeTest
    {      
        
        [TestMethod]
        public void Test_GetCartridgeList()
        {
            string output = Node.GetCartridgeList(true, true, false);
            Assert.IsNotNull(output);
        }

        [TestMethod]
        public void Test_ApplicationContainer()
        {
            bool testresults = true;
            try
            {
                using (ShimsContext.Create())
                {
                    ShimApplicationContainer container = new ShimApplicationContainer(TestHelper.CreateAppContainer());
                    container.CreateString = new FakesDelegates.Func<string, string>((string a) =>
                    {
                        ShimContainerPlugin containerPlugin = new ShimContainerPlugin(new ContainerPlugin(container.Instance));
                        containerPlugin.Create = new FakesDelegates.Action(() =>
                        {
                            Guid prisonGuid = PrisonIdConverter.Generate(container.Instance.Uuid);

                            Logger.Debug("Creating prison with guid: {0}", prisonGuid);

                            ShimPrison prison = new ShimPrison(new Uhuru.Prison.Prison(prisonGuid));
                            prison.Instance.Tag = "oo";

                            Uhuru.Prison.PrisonRules prisonRules = new Uhuru.Prison.PrisonRules();

                            prisonRules.CellType = Prison.RuleType.None;
                            prisonRules.CellType |= Prison.RuleType.Memory;
                            prisonRules.CellType |= Prison.RuleType.CPU;
                            prisonRules.CellType |= Prison.RuleType.WindowStation;
                            prisonRules.CellType |= Prison.RuleType.Httpsys;
                            prisonRules.CellType |= Prison.RuleType.IISGroup;

                            prisonRules.CPUPercentageLimit = Convert.ToInt64(Node.ResourceLimits["cpu_quota"]);
                            prisonRules.ActiveProcessesLimit = Convert.ToInt32(Node.ResourceLimits["max_processes"]);
                            prisonRules.PriorityClass = ProcessPriorityClass.Normal;

                            // TODO: vladi: make sure these limits are ok being the same
                            prisonRules.NetworkOutboundRateLimitBitsPerSecond = Convert.ToInt64(Node.ResourceLimits["max_upload_bandwidth"]);
                            prisonRules.AppPortOutboundRateLimitBitsPerSecond = Convert.ToInt64(Node.ResourceLimits["max_upload_bandwidth"]);

                            prisonRules.TotalPrivateMemoryLimitBytes = Convert.ToInt64(Node.ResourceLimits["max_memory"]) * 1024 * 1024;
                            prisonRules.DiskQuotaBytes = Convert.ToInt64(Node.ResourceLimits["quota_blocks"]) * 1024;

                            prisonRules.PrisonHomePath = container.Instance.ContainerDir;

                            //NodeConfig.Values["PORTS_PER_USER"] should be used DistrictConfig.Values["first_uid"] if available in the configuration           
                            try
                            {
                                try
                                {
                                    prisonRules.UrlPortAccess = Network.GetAvailablePort(container.Instance.uid, NodeConfig.Values["GEAR_BASE_DIR"], Int32.Parse(NodeConfig.Values["PORTS_PER_USER"]), Int32.Parse(DistrictConfig.Values["first_uid"]), Int32.Parse(NodeConfig.Values["STARTING_PORT"]));
                                }
                                catch
                                {
                                    prisonRules.UrlPortAccess = Network.GetAvailablePort(container.Instance.uid, NodeConfig.Values["GEAR_BASE_DIR"], Int32.Parse(NodeConfig.Values["PORTS_PER_USER"]), 0, Int32.Parse(NodeConfig.Values["STARTING_PORT"]));
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Debug("GetAvailablePort could not be called with all arguments: {0}", ex.Message.ToString());
                                prisonRules.UrlPortAccess = Network.GetAvailablePort(container.Instance.uid, NodeConfig.Values["GEAR_BASE_DIR"]);
                            }
                            //prisonRules.UrlPortAccess = Network.GetUniquePredictablePort(@"c:\openshift\ports");

                            Logger.Debug("Assigning port {0} to gear {1}", prisonRules.UrlPortAccess, container.Instance.Uuid);

                            prison.LockdownPrisonRules = new FakesDelegates.Action<Prison.PrisonRules>((Uhuru.Prison.PrisonRules rules) => {                                
                                
                            });

                            prison.Instance.Lockdown(prisonRules);

                            // Configure SSHD for the new prison user
                            string binLocation = Path.GetDirectoryName(this.GetType().Assembly.Location);
                            string configureScript = Path.GetFullPath(Path.Combine(binLocation, @"powershell\Tools\sshd\configure-sshd.ps1"));

                            Sshd.ConfigureSshd(NodeConfig.Values["SSHD_BASE_DIR"], container.Instance.Uuid, Environment.UserName, container.Instance.ContainerDir, NodeConfig.Values["GEAR_SHELL"]);
                                                        
                            container.Instance.InitializeHomedir(container.Instance.BaseDir, container.Instance.ContainerDir);

                            container.Instance.AddEnvVar("PRISON_PORT", prisonRules.UrlPortAccess.ToString());

                            LinuxFiles.TakeOwnershipOfGearHome(container.Instance.ContainerDir, Environment.UserName);
                        });
                        containerPlugin.Instance.Create();
                        return string.Empty;
                    });
                    container.Instance.Create();
                    if (container.Instance.StopLock==true)
                    {
                        testresults = false;
                    }
                    Uhuru.Openshift.Runtime.Model.GearRegistry reg = container.Instance.GearRegist;
                    ShimApplicationContainer test2 = new ShimApplicationContainer(ApplicationContainer.GetFromUuid(container.Instance.Uuid));
                    if(test2.Instance == null)
                    {
                        testresults = false;
                    }
                    AuthorizedKeysFile file = new AuthorizedKeysFile(container.Instance);
                    if (file == null)
                    {
                        testresults = false;
                    }
                    List<SshKey> keys = new List<SshKey>();
                    SshKey key = new SshKey();
                    key.Comment = "testComment";
                    key.Key = "TestKey";
                    keys.Add(key);
                    file.ReplaceKeys(keys);

                    container.Instance.AddSshKey("myKey", string.Empty, "CommentTest");
                    container.Instance.AddSshKeys(keys);
                    
                    Dictionary<string, string> TestDict = new Dictionary<string, string>();
                    TestDict.Add("test", "testvalue");
                    List<string> UserVarTest = new List<string>() { "Hello", "World" };

                    container.Instance.AddUserVar(TestDict, UserVarTest);
                    container.Instance.AddEnvVar("testKey", "testValue");
                    container.Instance.AddEnvVar("testKey1", "testValue", true);

                    if (container.Instance.AllDeploymentsByActivation().Count == 0)
                    {
                        testresults = false;
                    }
                    //TO DO:Investigate this
                    //RubyHash options = new RubyHash();
                    //options["deployment_id"] = "1";
                    //container.Instance.Activate(options);
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
