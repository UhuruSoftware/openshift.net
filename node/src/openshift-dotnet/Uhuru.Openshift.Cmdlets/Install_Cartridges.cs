using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Utilities;

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
                    Build(path);
                    Manifest manifest = repository.Install(path);                    
                    Logger.Info("Installed cartridge ({0}, {1}, {2}, {3}) from {4}", manifest.CartridgeVendor, manifest.Name, manifest.Version, manifest.CartridgeVersion, path);
                }
                catch(Exception e)
                {
                    Logger.Warning("Failed to install cartridge from {0}. {1}", path, e.ToString());                    
                }
            }
        }

        private static void Build(string path)
        {
            if (Directory.Exists(Path.Combine(path, "src")))
            {
                Logger.Debug(ProcessExtensions.RunCommandAndGetOutput(@"C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe", "", Path.Combine(path, "src")).StdOut);
                foreach(string assembly in Directory.GetFiles(Path.Combine(path, "bin"), "*.dll"))
                {
                    Logger.Debug(ProcessExtensions.RunCommandAndGetOutput(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\ngen.exe", string.Format("install {0}", assembly)).StdOut);
                }
                foreach (string assembly in Directory.GetFiles(Path.Combine(path, "bin"), "*.exe"))
                {
                    Logger.Debug(ProcessExtensions.RunCommandAndGetOutput(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\ngen.exe", string.Format("install {0}", assembly)).StdOut);
                }
            }
        }
    }
}
