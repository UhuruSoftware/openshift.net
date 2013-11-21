using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Uhuru.Openshift.Runtime.Utils;

namespace Uhuru.Openshift.Runtime
{
    public class CartridgeModel
    {
        public string StopLock
        {
            get
            {
                return Path.Combine(this.container.ContainerDir, "app-root", "runtime", ".stop_lock");
            }
        }

        public bool StopLockExists
        {
            get
            {
                return File.Exists(this.StopLock);
            }
        }

        private ApplicationContainer container;
        private ApplicationState state;
        private Hourglass hourglass;
        private int timeout;
        private List<Manifest> cartridges;        

        public CartridgeModel(ApplicationContainer container, ApplicationState state, Hourglass hourglass)
        {
            this.container = container;
            this.state = state;
            this.hourglass = hourglass;
            this.timeout = 30;
            this.cartridges = new List<Manifest>();
        }

        public string Configure(string cartName, string templateGitUrl, string manifest)
        {
            Manifest cartridge = new Manifest();
            CreateCartridgeDirectory(cartridge, null);
            return PopulateGearRepo(cartName, templateGitUrl);
        }

        public string StopGear(dynamic options)
        {
            return StopCartridge(new Manifest(), options);
        }

        public string StartGear(dynamic options)
        {
            return StartCartridge("start", new Manifest(), options);
        }

        public string StopCartridge(Manifest cartridge, dynamic options)
        {
            DoControl("stop", new Manifest(), options);
            return string.Empty;
        }

        public string StartCartridge(string action, Manifest cartridge, dynamic options)
        {
            DoControl(action, cartridge, options);
            return string.Empty;
        }

        public void DoControl(string action, Manifest cartridge, dynamic options)
        {
            DoControlWithDirectory(action, options);
        }

        public void DoControlWithDirectory(string action, dynamic options)
        {

        }

        private string PopulateGearRepo(string cartName, string templateGitUrl)
        {
            ApplicationRepository repo = new ApplicationRepository(this.container);
            repo.PopulateFromCartridge(cartName);
            if (repo.Exists())
            {
                repo.Archive(Path.Combine(this.container.ContainerDir, "app-root", "runtime", "repo"), "master");
            }
            return string.Empty;
        }

        private void CreateCartridgeDirectory(Manifest cartridge, string softwareVersion)
        {
            string target = Path.Combine(this.container.ContainerDir, cartridge.Dir);
            CartridgeRepository.InstantiateCartridge(cartridge, target);
        }
    }
}
