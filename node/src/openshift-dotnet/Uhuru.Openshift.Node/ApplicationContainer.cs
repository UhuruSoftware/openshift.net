using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime.Utils;

namespace Uhuru.Openshift.Runtime
{
    public class ApplicationContainer
    {
        public string Uuid { get; set; }
        public string ApplicationUuid { get; set; }
        public string ContainerName { get; set; }
        public string ApplicationName { get; set; }
        public string Namespace { get; set; }
        public object QuotaBlocks { get; set; }
        public object QuotaFiles { get; set; }
        public string ContainerDir { get { return @"C:\cygwin\administrator_home"; } }
        public CartridgeModel Cartridge { get; set; }
        public Hourglass GetHourglass { get { return this.hourglass; } }
        public ApplicationState State { get; set; }
        
        private Hourglass hourglass;

        [Obsolete("Used only for testing")]
        public ApplicationContainer() 
        {
            this.Cartridge = new CartridgeModel(this, this.State, this.hourglass);
            this.ApplicationName = "testnet";
            this.ApplicationUuid = Guid.NewGuid().ToString();
        }

        public ApplicationContainer(string applicationUuid, string containerUuid, string userId, string applicationName,
            string containerName, string namespaceName, object quotaBlocks, object quotaFiles, Hourglass hourglass)
        {
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
        }

        public string Create()
        {
            return string.Empty;
        }

        public string Destroy()
        {
            return string.Empty;
        }

        public string Configure(string cartName, string templateGitUrl, string manifest)        
        {
            return Cartridge.Configure(cartName, templateGitUrl, manifest);
        }

        public string PostConfigure()
        {
            return string.Empty;
        }

        public string AddSshKey(string sshKey, string keyType, string comment)
        {
            string output = "";

            string key = string.Format("{0} {1} {2}", keyType, sshKey, comment);

            string binLocation = Path.GetDirectoryName(this.GetType().Assembly.Location);
            string configureScript = Path.GetFullPath(Path.Combine(binLocation, @"..\src\openshift-dotnet\Uhuru.PowerShell\Tools\sshd\configure-sshd.ps1"));
            string addKeyScript = Path.GetFullPath(Path.Combine(binLocation, @"..\src\openshift-dotnet\Uhuru.PowerShell\Tools\sshd\add-key.ps1"));

            ProcessStartInfo pi = new ProcessStartInfo();
            pi.UseShellExecute = false;
            pi.RedirectStandardError = true;
            pi.RedirectStandardOutput = true; pi.FileName = "powershell.exe";
            pi.Arguments = string.Format(@"-ExecutionPolicy Bypass -InputFormat None -noninteractive -file {0} -targetDirectory c:\cygwin\installation\ -user {1} -windowsUser administrator -userHomeDir c:\cygwin\administrator_home", configureScript, this.ApplicationUuid);
            Process p = Process.Start(pi);
            p.WaitForExit(60000);
            output += this.ApplicationUuid;
            output += p.StandardError.ReadToEnd();
            output += p.StandardOutput.ReadToEnd();

            pi.Arguments = string.Format(@"-ExecutionPolicy Bypass -InputFormat None -noninteractive -file {0} -targetDirectory c:\cygwin\installation\ -windowsUser administrator -key ""{1}""", addKeyScript, key);
            p = Process.Start(pi);
            p.WaitForExit(60000);
            output += p.StandardError.ReadToEnd();
            output += p.StandardOutput.ReadToEnd();           

            return output;
        }

        public void PreReceive(dynamic options)
        {
            options["excludeWebProxy"] = true;
            options["userInitiated"] = true;
            StopGear(options);
        }

        public void PostReceive(dynamic options)
        {
            Dictionary<string, string> gearEnv = Environ.ForGear(this.ContainerDir);


            Distribute(options);
            Activate(options);
        }

        public void Distribute(dynamic options)
        {

        }

        public void Activate(dynamic options)
        {
            Dictionary<string, object> opts = new Dictionary<string, object>();
            opts["secondaryOnly"] = true;
            opts["userInitiated"] = true;
            opts["hotDeploy"] = options["hotDeploy"];
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
            this.Cartridge.StopGear(options);
            return string.Empty;
        }
    }
}
