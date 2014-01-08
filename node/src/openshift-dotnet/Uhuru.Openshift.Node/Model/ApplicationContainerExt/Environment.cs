using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Uhuru.Openshift.Runtime.Config;

namespace Uhuru.Openshift.Runtime
{
    
    public partial class ApplicationContainer
    {
        /// <summary>
        /// Retrieve user environment variable(s)
        /// </summary>
        public Dictionary<string, string> UserVarList(string[] variables)
        {  
            string userEnvDir = Path.Combine(ContainerDir, ".env", "user_vers");
            if (!Directory.Exists(userEnvDir))
            {
                return new Dictionary<string, string>();
            }

            Dictionary<string, string> env = Openshift.Runtime.Utils.Environ.Load(userEnvDir);
            if (variables == null || variables.Length == 0)
            {
                return env;
            }

            Dictionary<string, string> output = new Dictionary<string, string>();
            foreach (string variable in variables)
            {
                output.Add(variable, env[variable]);
            }
            return output;
        }

        public string RemoveSshKey(string sshKey, string keyType, string comment)
        {
            string output = "";

            string key = string.Format("{0} {1} {2}", keyType, sshKey, comment);
            string binLocation = Path.GetDirectoryName(this.GetType().Assembly.Location);
            string addKeyScript = Path.GetFullPath(Path.Combine(binLocation, @"powershell\Tools\sshd\remove-key.ps1"));

            ProcessStartInfo pi = new ProcessStartInfo();
            pi.UseShellExecute = false;
            pi.RedirectStandardError = true;
            pi.RedirectStandardOutput = true; pi.FileName = "powershell.exe";

            pi.Arguments = string.Format(@"-ExecutionPolicy Bypass -InputFormat None -noninteractive -file {0} -targetDirectory {2} -windowsUser administrator -key ""{1}""", addKeyScript, key, NodeConfig.Values["SSHD_BASE_DIR"]);
            Process p = Process.Start(pi);
            p.WaitForExit(60000);
            output += p.StandardError.ReadToEnd();
            output += p.StandardOutput.ReadToEnd();

            return output;
        }
    }
}
