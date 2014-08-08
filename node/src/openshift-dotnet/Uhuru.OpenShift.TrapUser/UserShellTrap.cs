using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Utils;
using Uhuru.Openshift.Utilities;


namespace Uhuru.OpenShift.TrapUser
{
    public class UserShellTrap
    {
        public static readonly IList<string> PassEnvs = new ReadOnlyCollection<string>(new[]
            {
                "GIT_SSH",
                "SSH_AUTH_SOCK",
                "SSH_CLIENT",
                "SSH_CONNECTION"
            });

        private static void LoadEnv(string directory, Dictionary<string, string> targetList)
        {
            Logger.Info("oo-trap-user loading env vars from directory '{0}'", directory);

            if (targetList == null)
            {
                throw new ArgumentNullException("targetList");
            }

            if (!Directory.Exists(directory))
            {
                return;
            }

            string[] envFiles = Directory.GetFiles(directory);

            StringBuilder logMessage = new StringBuilder();

            foreach (string envFile in envFiles)
            {
                string varValue = File.ReadAllText(envFile);
                string varKey = Path.GetFileName(envFile);
                targetList[varKey] = varValue;

                logMessage.AppendLine(string.Format("oo-trap-user loading env var '{0}' with value '{1}' from directory '{2}'", envFile, varValue, directory));
            }

            Logger.Info(logMessage.ToString());
        }

        public static void SetupGearEnv(Dictionary<string, string> targetList, string homeDir)
        {
            Logger.Info("oo-trap-user setting up vars for home directory '{0}'", homeDir);

            if (targetList == null)
            {
                throw new ArgumentNullException("targetList");
            }

            string globalEnv = Path.Combine(NodeConfig.ConfigDir, "env");

            UserShellTrap.LoadEnv(globalEnv, targetList);

            UserShellTrap.LoadEnv(Path.Combine(homeDir, ".env"), targetList);

            foreach (string dir in Directory.GetDirectories(Path.Combine(homeDir, ".env"), "*"))
            {
                LoadEnv(dir, targetList);
            }

            string[] userHomeDirs = Directory.GetDirectories(homeDir, "*", SearchOption.TopDirectoryOnly);

            foreach (string userHomeDir in userHomeDirs)
            {
                LoadEnv(Path.Combine(userHomeDir, "env"), targetList);
            }
        }

        public static int StartShell(string args)
        {
            string assemblyLocation = Path.GetDirectoryName(typeof(UserShellTrap).Assembly.Location);

            Dictionary<string, string> envVars = new Dictionary<string, string>();
            foreach(DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                if (PassEnvs.Contains(de.Key))
                {
                    envVars[de.Key.ToString()] = de.Value.ToString();
                }
            }

            string homeDir = Environment.GetEnvironmentVariable("HOME");

            SetupGearEnv(envVars, homeDir);

            envVars["CYGWIN"] = "nodosfilewarning winsymlinks:native";
            envVars["TEMP"] = Path.Combine(envVars["OPENSHIFT_HOMEDIR"], ".tmp");
            envVars["TMP"] = envVars["TEMP"];

            string arguments = string.Empty;
            if (args.StartsWith("\""))
            {
                arguments = Regex.Replace(args, @"\A""[^""]+""\s", "");
            }
            else
            {
                arguments = Regex.Replace(args, @"\A[^\s]+", "");
            }

            string bashBin = Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], @"bin\bash.exe");
            string gearUuid = envVars.ContainsKey("OPENSHIFT_GEAR_UUID") ? envVars["OPENSHIFT_GEAR_UUID"] : string.Empty;

            int exitCode = 0;

            if (Environment.UserName.StartsWith(Prison.PrisonUser.GlobalPrefix))
            {
                ProcessStartInfo shellStartInfo = new ProcessStartInfo();

                // System.Diagnostics.Process will merge the specified envs with the current existing ones
                foreach (string key in envVars.Keys)
                {
                    shellStartInfo.EnvironmentVariables[key] = envVars[key];
                }

                shellStartInfo.FileName = bashBin;
                shellStartInfo.Arguments = string.Format(@"--norc --login --noprofile {0}", arguments);
                shellStartInfo.UseShellExecute = false;
                Logger.Debug("Started trapped bash for gear {0}", gearUuid);
                Process shell = Process.Start(shellStartInfo);
                shell.WaitForExit();
                exitCode = shell.ExitCode;
                Logger.Debug("Process '{0}' exited with code '{1}'", shell.Id, shell.ExitCode);                 
            }
            else
            {
                string userHomeDir = envVars.ContainsKey("OPENSHIFT_HOMEDIR") && Directory.Exists(envVars["OPENSHIFT_HOMEDIR"]) ? envVars["OPENSHIFT_HOMEDIR"] : string.Empty;

                var prison = Prison.Prison.LoadPrisonAndAttach(PrisonIdConverter.Generate(gearUuid));

                FixHomeDir(userHomeDir, prison.User.Username, gearUuid);

                Logger.Debug("Starting trapped bash for gear {0}", gearUuid);
                var process = prison.Execute(bashBin, string.Format("--norc --login --noprofile {0}", arguments), false, envVars);
                process.WaitForExit();
                exitCode = process.ExitCode;
                Logger.Debug("Process '{0}' exited with code '{1}'", process.Id, exitCode);                 
            }

            return exitCode;
        }

        public static void FixHomeDir(string userHomeDir, string username, string gearUuid)
        {
            if (!string.IsNullOrEmpty(userHomeDir))
            {
                LinuxFiles.TakeOwnershipOfGearHome(userHomeDir, username);

                Logger.Debug("Fixing symlinks for gear {0}", gearUuid);
                try
                {
                    LinuxFiles.FixSymlinks(Path.Combine(userHomeDir, "app-deployments"));
                }
                catch (Exception ex)
                {
                    Logger.Error("There was an error while trying to fix symlinks for gear {0}: {1} - {2}", gearUuid, ex.Message, ex.StackTrace);
                }
            }
            else
            {
                Logger.Warning("Not taking ownership or fixing symlinks for gear {0}. Could not locate its home directory.", gearUuid);
            }
        }
    }
}
