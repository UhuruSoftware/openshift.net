using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Uhuru.Openshift.Runtime.Config;

namespace Uhuru.OpenShift.TrapUser
{
    public class UserShellTrap
    {
        private static void LoadEnv(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            string[] envFiles = Directory.GetFiles(directory);

            foreach (string envFile in envFiles)
            {
                string varValue = File.ReadAllText(envFile);
                string varKey = Path.GetFileName(envFile);
                Environment.SetEnvironmentVariable(varKey, varValue);
            }
        }

        public static void SetupGearEnv()
        {
            string globalEnv = Path.Combine(NodeConfig.ConfigDir, "env");
            UserShellTrap.LoadEnv(globalEnv);

            UserShellTrap.LoadEnv(".env");

            string[] userHomeDirs = Directory.GetDirectories(".\\", "*", SearchOption.TopDirectoryOnly);

            foreach (string userHomeDir in userHomeDirs)
            {
                LoadEnv(Path.Combine(userHomeDir, "env"));
            }
        }

        public static void StartShell()
        {
            string assemblyLocation = Path.GetDirectoryName(typeof(UserShellTrap).Assembly.Location);
            string rcfile = Path.Combine(assemblyLocation, @"mcollective\cmdlets\powershell-alias.sh");

            ProcessStartInfo shellStartInfo = new ProcessStartInfo();
            shellStartInfo.FileName = string.Format(@"bash", rcfile);
            shellStartInfo.Arguments = string.Format(@"--rcfile ""{0}""", rcfile);
            shellStartInfo.UseShellExecute = false;

            Process shell = Process.Start(shellStartInfo);

            shell.WaitForExit();
        }
    }
}
