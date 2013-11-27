using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Uhuru.Openshift.Common.Models;
using Uhuru.Openshift.Runtime.Config;
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
        private Dictionary<string, Manifest> cartridges;        

        public CartridgeModel(ApplicationContainer container, ApplicationState state, Hourglass hourglass)
        {
            this.container = container;
            this.state = state;
            this.hourglass = hourglass;
            this.timeout = 30;
            this.cartridges = new Dictionary<string, Manifest>();
        }

        public string Configure(string cartName, string templateGitUrl, string manifest)
        {   
            
            string name = cartName.Split('-')[0];
            string version = cartName.Split('-')[1];
            Manifest cartridge = null;
            if (!string.IsNullOrEmpty(manifest))
            {
                cartridge = new Manifest(manifest, version, null, NodeConfig.Values["CARTRIDGE_BASE_PATH"], true);
            }
            else
            {
                foreach(Cartridge cart in CartridgeRepository.Instance.LatestVersions)
                {
                    if(cart.OriginalName == name && cart.Version == version)
                    {
                        cartridge = new Manifest(cart.Spec, version, null, NodeConfig.Values["CARTRIDGE_BASE_PATH"], true);
                        break;
                    }
                }
            }
            CreateCartridgeDirectory(cartridge, version);
            return PopulateGearRepo(name, templateGitUrl);
        }

        public string StopGear(dynamic options)
        {
            EachCartridge(delegate(Manifest cartridge)
            {
                StopCartridge(cartridge, options);
            });            
            return string.Empty;
        }

        public delegate void ProcessCartridgeCallback(string cartDir);
        public delegate void EachCartridgeCallback(Manifest cartridge);

        private void EachCartridge(EachCartridgeCallback action)
        {
            ProcessCartridges(null,
                delegate(string cartridgeDir)
                {
                    Manifest cartridge = GetCartridgeFromDirectory(cartridgeDir);
                    action(cartridge);
                });
        }

        private void ProcessCartridges(string cartridgeDir, ProcessCartridgeCallback action)
        {
            if (!string.IsNullOrEmpty(cartridgeDir))
            {
                string cartDir = Path.Combine(this.container.ContainerDir, cartridgeDir);
                action(cartDir);
                return;
            }
            else
            {
                foreach (string dir in Directory.GetDirectories(container.ContainerDir))
                {
                    if(File.Exists(Path.Combine(dir, "metadata", "manifest.yml")))
                    {
                        action(dir);
                    }
                }
            }
        }


        public string StartGear(dynamic options)
        {
            EachCartridge(delegate(Manifest cartridge)
            {
                StartCartridge("start", cartridge, options);
            });
            return string.Empty;
        }

        public string StopCartridge(Manifest cartridge, dynamic options)
        {
            DoControl("stop", cartridge, options);
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

        public string DoControlWithDirectory(string action, dynamic options)
        {
            string control = Path.Combine(this.container.ContainerDir, "dotnet", "bin", "control.ps1");

            string binLocation = Path.GetDirectoryName(this.GetType().Assembly.Location);
            ProcessStartInfo pi = new ProcessStartInfo();
            pi.EnvironmentVariables.Add("OPENSHIFT_DOTNET_PORT", "80");
            pi.UseShellExecute = false;
            pi.CreateNoWindow = true;
            pi.RedirectStandardError = true;
            pi.RedirectStandardOutput = true; 
            pi.FileName = "powershell.exe";
            pi.Arguments = string.Format(@"-ExecutionPolicy Bypass -InputFormat None -noninteractive -file {0} -command {1}", control, action);
            Process p = new Process();
            p.StartInfo = pi;
            StringBuilder stdout = new StringBuilder();
            StringBuilder stderr = new StringBuilder();

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                p.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        stdout.AppendLine(e.Data);
                    }
                };
                p.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        stderr.AppendLine(e.Data);
                    }
                };

                p.Start();

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (p.WaitForExit(10000) &&
                    outputWaitHandle.WaitOne(10000) &&
                    errorWaitHandle.WaitOne(10000))
                {
                    // Process completed. Check process.ExitCode here.
                }
                else
                {
                    // Timed out.
                }
            }

            return stdout.ToString() + stderr.ToString(); ;
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

        public string PostConfigure(string cartridgeName)
        {
            string output = string.Empty;
            
            if (EmptyRepository())
            {
                output += "CLIENT_MESSAGE: An empty Git repository has been created for your application.  Use 'git push' to add your code.";
            }
            else
            {
                //output = this.StartCartridge("start",)
            }


            return output;
        }

        public Manifest GetCartridge(string cartName)
        {
            if (!cartridges.ContainsKey(cartName))
            {
                string cartDir = CartridgeDirectory(cartName);
                this.cartridges[cartName] = GetCartridgeFromDirectory(cartDir);
            }
            return cartridges[cartName];
        }

        public string CartridgeDirectory(string cartName)
        {
            return string.Empty;
        }

        public Manifest GetCartridgeFromDirectory(string cartDir)
        {
            string cartPath = Path.Combine(container.ContainerDir, cartDir);
            string manifestPath = Path.Combine(cartPath, "metadata", "manifest.yml");
            string manifest = File.ReadAllText(manifestPath);
            Manifest cartridge = new Manifest(manifest, null, null, NodeConfig.Values["CARTRIDGE_BASE_PATH"], true);
            this.cartridges[cartDir] = cartridge;
            return cartridge;
        }

        private void CreateCartridgeDirectory(Manifest cartridge, string softwareVersion)
        {
            string target = Path.Combine(this.container.ContainerDir, cartridge.Dir);
            CartridgeRepository.InstantiateCartridge(cartridge, target);

            string ident = Manifest.BuildIdent(cartridge.CartridgeVendor, cartridge.Name, softwareVersion, cartridge.CartridgeVersion);
            Dictionary<string, string> envs = new Dictionary<string, string>();
            envs[string.Format("{0}_DIR", cartridge.ShortName)] = target + Path.DirectorySeparatorChar;
            envs[string.Format("{0}_IDENT", cartridge.ShortName)] = ident;
            WriteEnvironmentVariables(Path.Combine(target, "env"), envs);
            envs = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(this.container.Namespace))
            {
                envs["namespace"] = this.container.Namespace;
            }


        }

        private void WriteEnvironmentVariables(string path, Dictionary<string,string> envs, bool prefix)
        {
            Directory.CreateDirectory(path);
            foreach (KeyValuePair<string, string> pair in envs)
            {
                string name = pair.Key.ToUpper();
                if (prefix)
                {
                    name = string.Format("OPENSHIFT_{0}", name);
                }
                File.WriteAllText(Path.Combine(path, name), pair.Value);
            }
        }

        private void WriteEnvironmentVariables(string path, Dictionary<string, string> envs)
        {
            WriteEnvironmentVariables(path, envs, true);
        }

        private bool EmptyRepository()
        {
            return new ApplicationRepository(this.container).Empty();
        }
    }
}
