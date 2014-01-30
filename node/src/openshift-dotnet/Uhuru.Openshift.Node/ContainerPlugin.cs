using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            Guid prisonGuid = Guid.Parse(container.Uuid.PadLeft(32, '0'));

            Logger.Debug("Creating prison with guid: {0}", prisonGuid);

            Uhuru.Prison.Prison prison = new Uhuru.Prison.Prison(prisonGuid);
            prison.Tag = "oo";

            Uhuru.Prison.PrisonRules prisonRules = new Uhuru.Prison.PrisonRules();

            prisonRules.CellType = Prison.RuleType.None;
            prisonRules.CellType |= Prison.RuleType.WindowStation;
            prisonRules.CellType |= Prison.RuleType.Httpsys;
            prisonRules.CellType |= Prison.RuleType.IISGroup;
            prisonRules.CellType |= Prison.RuleType.MsSqlInstance;


            prisonRules.PrisonHomePath = container.ContainerDir;
            prisonRules.UrlPortAccess = Network.GetUniquePredictablePort(@"c:\openshift\ports");

            Logger.Debug("Assigning port {0} to gear {1}", prisonRules.UrlPortAccess, container.Uuid);

            prison.Lockdown(prisonRules);

            // Configure SSHD for the new prison user
            string binLocation = Path.GetDirectoryName(this.GetType().Assembly.Location);
            string configureScript = Path.GetFullPath(Path.Combine(binLocation, @"powershell\Tools\sshd\configure-sshd.ps1"));

            ProcessResult result = ProcessExtensions.RunCommandAndGetOutput(ProcessExtensions.Get64BitPowershell(), string.Format(
@"-ExecutionPolicy Bypass -InputFormat None -noninteractive -file {0} -targetDirectory {2} -user {1} -windowsUser {5} -userHomeDir {3} -userShell {4}",
                configureScript,
                container.Uuid,
                NodeConfig.Values["SSHD_BASE_DIR"],
                container.ContainerDir,
                NodeConfig.Values["GEAR_SHELL"],
                Environment.UserName));

            if (result.ExitCode != 0)
            {
                throw new Exception(string.Format("Error setting up sshd for gear {0} - rc={1}; out={2}; err={3}", container.Uuid, result.ExitCode, result.StdOut, result.StdErr));
            }

            this.container.InitializeHomedir(this.container.BaseDir, this.container.ContainerDir);

            container.AddEnvVar("PRISON_PORT", prisonRules.UrlPortAccess.ToString());

            LinuxFiles.TakeOwnershipOfGearHome(this.container.ContainerDir, prison.User.Username);
        }

        public string Destroy()
        {
            string output = this.container.KillProcs();
            Directory.Delete(this.container.ContainerDir, true);
            return output;
        }

        public string Stop(dynamic options = null)
        {
            return this.container.KillProcs(options);
        }
    }
}
