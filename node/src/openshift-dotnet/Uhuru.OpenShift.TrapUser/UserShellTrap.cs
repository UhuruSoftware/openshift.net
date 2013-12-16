using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uhuru.Openshift.Runtime.Config;

namespace Uhuru.OpenShift.TrapUser
{
    public class UserShellTrap
    {
        private static void LoadEnv(string directory, StringDictionary targetList)
        {
            if (targetList == null)
            {
                throw new ArgumentNullException("targetList");
            }

            if (!Directory.Exists(directory))
            {
                return;
            }

            string[] envFiles = Directory.GetFiles(directory);

            foreach (string envFile in envFiles)
            {
                string varValue = File.ReadAllText(envFile);
                string varKey = Path.GetFileName(envFile);
                targetList[varKey] = varValue;
            }
        }

        private static void SetupGearEnv(StringDictionary targetList)
        {
            if (targetList == null)
            {
                throw new ArgumentNullException("targetList");
            }
  
            string globalEnv = Path.Combine(NodeConfig.ConfigDir, "env");
           
            UserShellTrap.LoadEnv(globalEnv, targetList);

            UserShellTrap.LoadEnv(".env", targetList);

            foreach (string dir in Directory.GetDirectories( ".env", "*"))
            {
                LoadEnv(dir, targetList);
            }

            string[] userHomeDirs = Directory.GetDirectories(".\\", "*", SearchOption.TopDirectoryOnly);

            foreach (string userHomeDir in userHomeDirs)
            {
                LoadEnv(Path.Combine(userHomeDir, "env"), targetList);
            }
        }

        public static void StartShell()
        {
            string assemblyLocation = Path.GetDirectoryName(typeof(UserShellTrap).Assembly.Location);
            string rcfile = Path.Combine(assemblyLocation, @"mcollective\cmdlets\powershell-alias.sh");

            ProcessStartInfo shellStartInfo = new ProcessStartInfo();
            shellStartInfo.EnvironmentVariables["CYGWIN"] = "nodosfilewarning";
            shellStartInfo.FileName = "bash";
            SetupGearEnv(shellStartInfo.EnvironmentVariables);

            string args = Environment.CommandLine;
            string arguments = string.Empty;
            if (args.StartsWith("\""))
            {
                arguments = Regex.Replace(args, @"\A""[^""]+""\s", "");
            }
            else
            {
                arguments = Regex.Replace(args, @"\A[^\s]+", "");
            }
            shellStartInfo.Arguments = string.Format(@"--rcfile ""{0}"" {1}", rcfile, arguments);
            shellStartInfo.UseShellExecute = false;

            Process shell = Process.Start(shellStartInfo);

            shell.WaitForExit();
        }
    }
}
