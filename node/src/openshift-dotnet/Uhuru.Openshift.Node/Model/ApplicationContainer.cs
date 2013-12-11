using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Utils;
using Uhuru.Openshift.Runtime.Model;

namespace Uhuru.Openshift.Runtime
{
    public partial class ApplicationContainer
    {
        public string Uuid { get; set; }
        public string ApplicationUuid { get; set; }
        public string ContainerName { get; set; }
        public string ApplicationName { get; set; }
        public string Namespace { get; set; }
        public string BaseDir { get; set; }

        public object QuotaBlocks { get; set; }
        public object QuotaFiles { get; set; }
        
        public string ContainerDir 
        { 
            get 
            { 
                return Path.Combine(NodeConfig.Values["GEAR_BASE_DIR"], this.Uuid);
            } 
        }
                
        public CartridgeModel Cartridge { get; set; }
        public Hourglass GetHourglass { get { return this.hourglass; } }
        public ApplicationState State { get; set; }

        ContainerPlugin containerPlugin;
        NodeConfig config;
        private Hourglass hourglass;

        public ApplicationContainer(string applicationUuid, string containerUuid, string userId, string applicationName,
            string containerName, string namespaceName, object quotaBlocks, object quotaFiles, Hourglass hourglass)
        {
            this.config = NodeConfig.Values;
            this.Uuid = containerUuid;
            this.ApplicationUuid = applicationUuid;
            this.ApplicationName = applicationName;
            this.ContainerName = containerName;
            this.Namespace = namespaceName;
            this.QuotaBlocks = quotaBlocks;
            this.QuotaFiles = quotaFiles;
            this.Cartridge = new CartridgeModel(this, this.State, this.hourglass);
            this.hourglass = hourglass ?? new Hourglass(3600);
            this.State = new ApplicationState(this);
            this.BaseDir = this.config["GEAR_BASE_DIR"];
            this.containerPlugin = new ContainerPlugin(this);
        }

        public string Create()
        {            
            containerPlugin.Create();
            return string.Empty;
        }

        public string Destroy()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(this.Cartridge.Destroy());
            output.AppendLine(this.containerPlugin.Destroy());
            return output.ToString();
        }

        public string KillProcs()
        {
            // TODO need to kill all user processes. stopping gear for now
            return this.StopGear(new Dictionary<string, object>());
        }

        public string Configure(string cartName, string templateGitUrl, string manifest)        
        {
            return Cartridge.Configure(cartName, templateGitUrl, manifest);
        }

        public string ConnectorExecute(string cartName, string hookName, string publishingCartName, string connectionType, string inputArgs)
        {
            // TODO: this method is not fully implemented - its Linux counterpart has extra functionality
            
            bool envVarHook = (connectionType.StartsWith("ENV:") && !string.IsNullOrEmpty(publishingCartName));

            if (envVarHook)
            {
                SetConnectionHookEnvVars(cartName, publishingCartName, inputArgs);
            }

            return "";
        }

        private void SetConnectionHookEnvVars(string cartName, string pubCartName, string args)
        {
            string envPath = Path.Combine(this.ContainerDir, ".env", CartridgeModel.ShortNameFromFullCartName(pubCartName));


            object[] argsObj = JsonConvert.DeserializeObject<object[]>(args);

            string envVars = (string)((Newtonsoft.Json.Linq.JObject)argsObj[3]).Properties().ElementAt(0).Value;

            string[] pairs = envVars.Split('\n');

            Dictionary<string, string> variables = new Dictionary<string,string>();

            foreach (string pair in pairs)
            {
                string[] keyAndValue = pair.Trim().Split('=');
                this.AddEnvVar(keyAndValue[0], keyAndValue[1]);
            }

            CartridgeModel.WriteEnvironmentVariables(envPath, variables, false);
        }


        public string PostConfigure()
        {
            string output = RunProcessInGearContext(this.ContainerDir, "gear -Prereceive -Init");
            output += RunProcessInGearContext(this.ContainerDir, "gear -Postreceive -Init");
            return output;
        }

        public string Start(string cartName, dynamic options = null)
        {
            if (options == null)
            {
                options = new Dictionary<string,object>();
            }
            return this.Cartridge.StartCartridge("start", cartName, options);
        }

        public string Stop(string cartName, dynamic options = null)
        {
            if (options == null)
            {
                options = new Dictionary<string, object>();
            }
            return this.Cartridge.StopCartridge(cartName, true, options);
        }

        public string AddSshKey(string sshKey, string keyType, string comment)
        {
            string output = "";

            string key = string.Format("{0} {1} {2}", keyType, sshKey, comment);

            string binLocation = Path.GetDirectoryName(this.GetType().Assembly.Location);
            string configureScript = Path.GetFullPath(Path.Combine(binLocation, @"powershell\Tools\sshd\configure-sshd.ps1"));
            string addKeyScript = Path.GetFullPath(Path.Combine(binLocation, @"powershell\Tools\sshd\add-key.ps1"));

            ProcessStartInfo pi = new ProcessStartInfo();            
            pi.UseShellExecute = false;
            pi.RedirectStandardError = true;
            pi.RedirectStandardOutput = true; pi.FileName = "powershell.exe";
            
            pi.Arguments = string.Format(
@"-ExecutionPolicy Bypass -InputFormat None -noninteractive -file {0} -targetDirectory {2} -user {1} -windowsUser administrator -userHomeDir {3} -userShell {4}", 
                configureScript, 
                this.ApplicationUuid, 
                NodeConfig.Values["SSHD_BASE_DIR"], 
                this.ContainerDir,
                NodeConfig.Values["GEAR_SHELL"]);

            Process p = Process.Start(pi);
            p.WaitForExit(60000);
            output += this.ApplicationUuid;
            output += p.StandardError.ReadToEnd();
            output += p.StandardOutput.ReadToEnd();

            pi.Arguments = string.Format(@"-ExecutionPolicy Bypass -InputFormat None -noninteractive -file {0} -targetDirectory {2} -windowsUser administrator -key ""{1}""", addKeyScript, key, NodeConfig.Values["SSHD_BASE_DIR"]);
            p = Process.Start(pi);
            p.WaitForExit(60000);
            output += p.StandardError.ReadToEnd();
            output += p.StandardOutput.ReadToEnd();           

            return output;
        }

        public string PreReceive(dynamic options)
        {
            options["excludeWebProxy"] = true;
            options["userInitiated"] = true;
            StopGear(options);
            CreateDeploymentDir();

            return string.Empty;
        }

        public void PostReceive(dynamic options)
        {
            Dictionary<string, string> gearEnv = Environ.ForGear(this.ContainerDir);

            string repoDir = Path.Combine(this.ContainerDir, "app-root", "runtime", "repo");

            Directory.CreateDirectory(repoDir);

            ApplicationRepository applicationRepository = new ApplicationRepository(this);
            applicationRepository.Archive(repoDir, "master");

            Distribute(options);
            Activate(options);
        }

        public void Distribute(dynamic options)
        {

        }



        private DateTime CreateDeploymentDir()
        {
            DateTime deploymentdateTime = DateTime.Now;

            string fullPath = Path.Combine(this.ContainerDir, "app-deployments", deploymentdateTime.ToString("yyyy-MM-dd_HH-mm-s"));
            Directory.CreateDirectory(Path.Combine(fullPath, "repo"));
            Directory.CreateDirectory(Path.Combine(fullPath, "dependencies"));
            Directory.CreateDirectory(Path.Combine(fullPath, "build-depedencies"));
            SetRWPermissions(fullPath);
            PruneDeployments();
            return deploymentdateTime;
        }

        private void PruneDeployments()
        {}



        public void Activate(dynamic options)
        {
            Dictionary<string, object> opts = new Dictionary<string, object>();
            opts["secondaryOnly"] = true;
            opts["userInitiated"] = true;
            //opts["hotDeploy"] = options["hotDeploy"];
            StartGear(opts);
        }

        private void StartGear(dynamic options)
        {
            this.Cartridge.StartGear(options);
        }


        private void ActivateLocalGear(dynamic options)
        {

        }

        public void SetRWPermissions(string filename)
        {

        }

        public string StopGear(dynamic options)
        {
            return this.Cartridge.StopGear(options);
        }

        public string RunProcessInGearContext(string gearDirectory, string cmd)
        {
            StringBuilder output = new StringBuilder();

            ProcessStartInfo pi = new ProcessStartInfo();
            pi.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH") + ";" + Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], "bin");
            pi.UseShellExecute = false;
            pi.CreateNoWindow = true;
            pi.RedirectStandardError = true;
            pi.RedirectStandardOutput = true;
            pi.WorkingDirectory = gearDirectory;
            pi.EnvironmentVariables["OPENSHIFT_DOTNET_PORT"] = "80";
            pi.FileName = Path.Combine(Path.GetDirectoryName(typeof(ApplicationContainer).Assembly.Location), "oo-trap-user.exe");
            pi.Arguments = "-c \"" + cmd.Replace('\\','/') + "\"";
            
            Process p = new Process();
            p.StartInfo = pi;

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
                        output.AppendLine(e.Data);
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
                        output.AppendLine(e.Data);
                    }
                };

                p.Start();

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (p.WaitForExit(30000) &&
                    outputWaitHandle.WaitOne(30000) &&
                    errorWaitHandle.WaitOne(30000))
                {
                    // Process completed. Check process.ExitCode here.
                }
                else
                {
                    // Timed out.
                }
            }
            return output.ToString();
        }


        internal void SetRoPermissions(string hooks)
        {
        }

        internal void InitializeHomedir(string baseDir, string homeDir)
        {
            Directory.CreateDirectory(Path.Combine(homeDir, ".tmp"));
            Directory.CreateDirectory(Path.Combine(homeDir, ".sandbox"));
            
            string sandboxUuidDir = Path.Combine(homeDir, ".sandbox", this.Uuid);
            Directory.CreateDirectory(sandboxUuidDir);
            SetRWPermissions(sandboxUuidDir);

            string envDir = Path.Combine(homeDir, ".env");
            Directory.CreateDirectory(envDir);
            SetRoPermissions(envDir);

            string userEnvDir = Path.Combine(homeDir, ".env", "user_vars");
            Directory.CreateDirectory(userEnvDir);
            SetRoPermissions(userEnvDir);

            string sshDir = Path.Combine(homeDir, ".ssh");
            Directory.CreateDirectory(sshDir);
            SetRoPermissions(sshDir);

            string gearDir = Path.Combine(homeDir, this.ContainerName);
            string gearAppDir = Path.Combine(homeDir, "app-root");

            AddEnvVar("APP_DNS", string.Format("{0}-{1}.{2}", this.ApplicationName, this.Namespace, this.config["CLOUD_DOMAIN"]), true);
            AddEnvVar("APP_NAME", this.ApplicationName, true);
            AddEnvVar("APP_UUID", this.ApplicationUuid, true);
            
            string dataDir = Path.Combine(gearAppDir, "data");
            Directory.CreateDirectory(dataDir);
            AddEnvVar("DATA_DIR", dataDir, true);

            string deploymentsDir = Path.Combine(homeDir, "app-deployments");
            Directory.CreateDirectory(deploymentsDir);
            AddEnvVar("DEPLOYMENTS_DIR", deploymentsDir, true);

            CreateDeploymentDir();

            AddEnvVar("GEAR_DNS", string.Format("{0}-{1}.{2}", this.ContainerName, this.Namespace, this.config["CLOUD_DOMAIN"]), true);
            AddEnvVar("GEAR_NAME", this.ContainerName, true);
            AddEnvVar("GEAR_UUID", this.Uuid, true);
            AddEnvVar("HOMEDIR", homeDir, true);
            AddEnvVar("HOME", homeDir, false);
            AddEnvVar("NAMESPACE", this.Namespace, true);

            string repoDir = Path.Combine(gearAppDir, "runtime", "repo");
            AddEnvVar("REPO_DIR", repoDir, true);
            Directory.CreateDirectory(repoDir);

            this.State.Value(Runtime.State.NEW);
        }

        private void AddEnvVar(string key, string value, bool prefixCloudName)
        {
            string envDir = Path.Combine(this.ContainerDir, ".env");
            if (prefixCloudName)
            {
                key = string.Format("OPENSHIFT_{0}", key);
            }
            string fileName = Path.Combine(envDir, key);
            File.WriteAllText(fileName, value);
            SetRoPermissions(fileName);
        }
        
        private void AddEnvVar(string key, string value)
        {
            AddEnvVar(key, value, false);
        }

        
    }
}
