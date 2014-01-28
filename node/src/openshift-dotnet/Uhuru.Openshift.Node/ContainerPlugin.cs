using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Uhuru.Openshift.Runtime.Config;

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
            Uhuru.Prison.Prison prison = new Uhuru.Prison.Prison(Guid.Parse(String.Format("{0:20:0}", container.Uuid)));
            prison.Tag = "oo";

            Uhuru.Prison.PrisonRules prisonRules = new Uhuru.Prison.PrisonRules();

            prisonRules.CellType = Uhuru.Prison.RuleType.None;
            prisonRules.CellType = Uhuru.Prison.RuleType.WindowStation;
            prisonRules.PrisonHomePath = container.ContainerDir;

            prison.Lockdown(prisonRules);

            this.container.InitializeHomedir(this.container.BaseDir, this.container.ContainerDir);
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
