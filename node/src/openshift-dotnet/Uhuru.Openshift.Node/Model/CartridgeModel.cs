using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            CreateCartridgeDirectory(cartridge, "4.5");
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

        public string DoControlWithDirectory(string action, dynamic options)
        {
            string control = Path.Combine(this.container.ContainerDir, "dotnet", "bin", "control.ps1");

            string binLocation = Path.GetDirectoryName(this.GetType().Assembly.Location);
            ProcessStartInfo pi = new ProcessStartInfo();
            pi.EnvironmentVariables.Add("OPENSHIFT_DOTNET_DIR", Path.Combine(this.container.ContainerDir, "dotnet"));
            pi.EnvironmentVariables.Add("OPENSHIFT_DOTNET_IP", "80");
            pi.EnvironmentVariables.Add("OPENSHIFT_REPO_DIR", Path.Combine(this.container.ContainerDir, "dotnet", "usr", "template"));
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
    }
}
