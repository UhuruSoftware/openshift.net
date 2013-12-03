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
            // create user

            this.container.InitializeHomedir(this.container.BaseDir, this.container.ContainerDir);
        }

        public string Destroy()
        {
            string output = this.container.KillProcs();
            Directory.Delete(this.container.ContainerDir, true);
            return output;
        }
    }
}
