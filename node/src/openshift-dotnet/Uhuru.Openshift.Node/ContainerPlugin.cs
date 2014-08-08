using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Uhuru.Openshift.Common.Utils;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Utils;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Runtime
{
    public class ContainerPlugin
    {
        private ApplicationContainer container;
        private NodeConfig config;

        public ContainerPlugin(ApplicationContainer applicationContainer)
        {
            this.container = applicationContainer;
            this.config = NodeConfig.Values;
        }

        public void Create()
        {
            Guid prisonGuid = PrisonIdConverter.Generate(container.Uuid);

            Logger.Debug("Creating prison with guid: {0}", prisonGuid);

            Uhuru.Prison.Prison prison = new Uhuru.Prison.Prison(prisonGuid);
            prison.Tag = "oo";

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

            prisonRules.PrisonHomePath = container.ContainerDir;
                        
            //NodeConfig.Values["PORTS_PER_USER"] should be used DistrictConfig.Values["first_uid"] if available in the configuration           
            try
            {
                try
                {
                    prisonRules.UrlPortAccess = Network.GetAvailablePort(container.uid, NodeConfig.Values["GEAR_BASE_DIR"], Int32.Parse(NodeConfig.Values["PORTS_PER_USER"]), Int32.Parse(DistrictConfig.Values["first_uid"]), Int32.Parse(NodeConfig.Values["STARTING_PORT"]));
                }
                catch
                {
                    prisonRules.UrlPortAccess = Network.GetAvailablePort(container.uid, NodeConfig.Values["GEAR_BASE_DIR"], Int32.Parse(NodeConfig.Values["PORTS_PER_USER"]), 0, Int32.Parse(NodeConfig.Values["STARTING_PORT"]));
                }
            }
            catch(Exception ex)
            {
                Logger.Debug("GetAvailablePort could not be called with all arguments: {0}", ex.Message.ToString());
                prisonRules.UrlPortAccess = Network.GetAvailablePort(container.uid, NodeConfig.Values["GEAR_BASE_DIR"]);
            }
            //prisonRules.UrlPortAccess = Network.GetUniquePredictablePort(@"c:\openshift\ports");

            Logger.Debug("Assigning port {0} to gear {1}", prisonRules.UrlPortAccess, container.Uuid);

            prison.Lockdown(prisonRules);

            // Configure SSHD for the new prison user
            string binLocation = Path.GetDirectoryName(this.GetType().Assembly.Location);
            string configureScript = Path.GetFullPath(Path.Combine(binLocation, @"powershell\Tools\sshd\configure-sshd.ps1"));

            Sshd.ConfigureSshd(NodeConfig.Values["SSHD_BASE_DIR"], container.Uuid, Environment.UserName, container.ContainerDir, NodeConfig.Values["GEAR_SHELL"]);

            this.container.InitializeHomedir(this.container.BaseDir, this.container.ContainerDir);

            container.AddEnvVar("PRISON_PORT", prisonRules.UrlPortAccess.ToString());

            LinuxFiles.TakeOwnershipOfGearHome(this.container.ContainerDir, prison.User.Username);
        }

        public string RemoveSshdUser()
        {
            Sshd.RemoveUser(NodeConfig.Values["SSHD_BASE_DIR"], container.Uuid, Environment.UserName, container.ContainerDir, NodeConfig.Values["GEAR_SHELL"]);
            return string.Empty;
        }

        public string Destroy()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(this.container.KillProcs());
            output.AppendLine(RemoveSshdUser());

            var prison = Prison.Prison.LoadPrisonAndAttach(PrisonIdConverter.Generate(this.container.Uuid));
            Logger.Debug("Destroying prison for gear {0}", this.container.Uuid);

            prison.Destroy();

            Directory.Delete(this.container.ContainerDir, true);
            return output.ToString();
        }

        public void KillAll(int termDelaySec)
        {
            var prison = Prison.Prison.LoadPrisonAndAttach(PrisonIdConverter.Generate(this.container.Uuid));
            Logger.Debug("Stopping prison for gear {0}", this.container.Uuid);

            var start = DateTime.Now;
            while (prison.JobObject.ActiveProcesses > 0 && (DateTime.Now - start).TotalSeconds < termDelaySec)
            {
                Thread.Sleep(200);
            }

            prison.ForceStop();
        }

        public string Stop(dynamic options = null)
        {
            return this.container.KillProcs(options);
        }
    }
}
