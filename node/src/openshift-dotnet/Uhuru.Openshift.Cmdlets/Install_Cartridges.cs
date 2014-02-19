using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Config;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("Install", "Cartridges")]
    public class Install_Cartridges: Cmdlet
    {

        [Parameter]
        public SwitchParameter V;

        protected override void ProcessRecord()
        {
            CartridgeRepository repository = CartridgeRepository.Instance;

            foreach(string path in Directory.GetDirectories(NodeConfig.Values["CARTRIDGE_BASE_PATH"]))
            {
                try
                {
                    Manifest manifest = repository.Install(path);
                    Logger.Info("Installed cartridge ({0}, {1}, {2}, {3}) from {4}", manifest.CartridgeVendor, manifest.Name, manifest.Version, manifest.CartridgeVersion, path);
                }
                catch(Exception e)
                {
                    Logger.Warning("Failed to install cartridge from {0}. {1}", path, e.ToString());                    
                }
            }
        }
    }
}
