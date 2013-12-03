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
            this.CartridgeName = cartName;            
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

        public Manifest GetPrimaryCartridge()
        {
            Dictionary<string,string> env = Environ.ForGear(container.ContainerDir);
            string primaryCartDir = null;
            if (env.ContainsKey("OPENSHIFT_PRIMARY_CARTRIDGE_DIR"))
            {
                primaryCartDir = env["OPENSHIFT_PRIMARY_CARTRIDGE_DIR"];
            }
            else
            {
                return null;
            }

            return GetCartridgeFromDirectory(primaryCartDir);
        }


        public string StopGear(dynamic options)
        {
            StringBuilder output = new StringBuilder();
            EachCartridge(delegate(Manifest cartridge)
            {
                output.AppendLine(StopCartridge(cartridge, options));
            });
            return output.ToString();
        }

        public delegate void ProcessCartridgeCallback(string cartDir);
        public delegate void EachCartridgeCallback(Manifest cartridge);

        public void EachCartridge(EachCartridgeCallback action)
        {
            ProcessCartridges(null,
                delegate(string cartridgeDir)
                {
                    Manifest cartridge = GetCartridgeFromDirectory(cartridgeDir);
                    action(cartridge);
                });
        }

        public string Destroy()
        {
            StringBuilder output = new StringBuilder();
            EachCartridge(delegate(Manifest cartridge)
                {
                    output.AppendLine(CartridgeTeardown(cartridge.Dir, false));
                });
            return output.ToString();
        }

        private string CartridgeTeardown(string cartridgeName, bool removeCartridgeDir)
        {
            string cartridgeHome = Path.Combine(this.container.ContainerDir, cartridgeName);
            Dictionary<string, string> env = Environ.ForGear(this.container.ContainerDir, cartridgeHome);
            string teardown = Path.Combine(cartridgeHome, "bin", "teardown.ps1");
            if (!File.Exists(teardown))
            {
                return string.Empty;
            }

            // run teardown script

            return string.Empty;
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
            StringBuilder output = new StringBuilder();
            EachCartridge(delegate(Manifest cartridge)
            {
                output.AppendLine(StartCartridge("start", cartridge, options));
            });
            return output.ToString();
        }


        public string StopCartridge(string cartridgeName, bool userInitiated, dynamic options)
        {

            Manifest manifest = GetCartridge(cartridgeName);
            if (!options.ContainsKey("user_initiated"))
            {
                options.Add("user_initiated", userInitiated);
            }
            else
            {
                options["user_initiated"] = userInitiated;
            }
            return StopCartridge(manifest, options);

        }

        public string StopCartridge(Manifest cartridge, dynamic options)
        {
            options = (Dictionary<string, object>)options;
            if (!options.ContainsKey("user_initiated"))
            {
                options.Add("user_initiated", true);
            }

            if (!options.ContainsKey("hot_deploy"))
            {
                options.Add("hot_deploy", false);
            }

            if (options["hot_deploy"])
            {
                return string.Format("Not stopping cartridge {0} because hot deploy is enabled", cartridge.Name);
            }

            if (!options["user_initiated"] && StopLockExists)
            {
                return string.Format("Not stopping cartridge {0} because the application was explicitly stopped by the user", cartridge.Name);
            }

            Manifest primaryCartridge = GetPrimaryCartridge();
            if (primaryCartridge != null)
            {
                if (cartridge.Name == primaryCartridge.Name)
                {
                    if (options["user_initiated"])
                    {
                        CreateStopLock();
                    }
                    state.Value(State.STOPPED);
                }
            }

            return DoControl("stop", cartridge, options);
        }
        
        public string StartCartridge(string action, Manifest cartridge, dynamic options)
        {
            options = (Dictionary<string, object>)options;
            if (!options.ContainsKey("user_initiated"))
            {
                options.Add("user_initiated", true);
            }

            if (!options.ContainsKey("hot_deploy"))
            {
                options.Add("hot_deploy", false);
            }

            if (!options["user_initiated"] && StopLockExists)
            {
                return string.Format("Not starting cartridge #{cartridge.name} because the application was explicitly stopped by the user", cartridge.Name);
            }

            Manifest primaryCartridge = GetPrimaryCartridge();

            if (primaryCartridge != null)
            {
                if (primaryCartridge.Name == cartridge.Name)
                {
                    if (options["user_initiated"])
                    {
                        File.Delete(StopLock);
                    }
                    state.Value(State.STARTED);

                //TODO : Unidle the application
                }
            }

            if (options["hot_deploy"])
            {
                return string.Format("Not starting cartridge {0} because hot deploy is enabled", cartridge.Name);
            }

            return DoControl(action, cartridge, options);
        }

        public string StartCartridge(string action, string cartridgeName, dynamic options)
        {
            Manifest manifest = GetCartridge(cartridgeName);
            return StartCartridge(action, manifest, options);

        }

        public string DoControl(string action, Manifest cartridge, dynamic options)
        {
            options["cartridgeDir"] = cartridge.Dir;
            return DoControlWithDirectory(action, options);
        }

        public string DoControlWithDirectory(string action, dynamic options)
        {
            StringBuilder output = new StringBuilder();
            string cartridgeDirectory = options["cartridgeDir"];
            
            ProcessCartridges(cartridgeDirectory, delegate(string cartridgeDir)
            {
                string control = Path.Combine(cartridgeDir, "bin", "control.ps1");
                string cmd = string.Format("powershell.exe -ExecutionPolicy Bypass -InputFormat None -noninteractive -file {0} -command {1}", control, action);
                
                output.AppendLine(container.RunProcessInGearContext(container.ContainerDir, cmd));               
            });
            return output.ToString();
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
            string name = cartName.Split('-')[0];
            string version = cartName.Split('-')[1];
            return Path.Combine(container.ContainerDir, name);
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

        public static void WriteEnvironmentVariables(string path, Dictionary<string,string> envs, bool prefix)
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

        public static void WriteEnvironmentVariables(string path, Dictionary<string, string> envs)
        {
            WriteEnvironmentVariables(path, envs, true);
        }

        private bool EmptyRepository()
        {
            return new ApplicationRepository(this.container).Empty();
        }

        public string CartridgeName { get; set; }

        public static string ShortNameFromFullCartName(string pubCartName)
        {
            if (string.IsNullOrEmpty(pubCartName))
            {
                throw new ArgumentNullException("pubCartName");
            }

            if (!pubCartName.Contains('-'))
            {
                return pubCartName;
            }

            string[] tokens = pubCartName.Split('-');

            return string.Join("-", tokens.Take(tokens.Length - 1));
        }
        
        private void CreateStopLock()
        {
            if (!StopLockExists)
            {
                File.Create(StopLock);
                container.SetRWPermissions(StopLock);
            }
        }
    }
}
