using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Control
{
    class Program
    {
        static string pidFile;

        static int Main(string[] args)
        {
            try
            {
                pidFile = Path.Combine(Environment.GetEnvironmentVariable("OPENSHIFT_DOTNET_DIR"), "run", "iishwc.pid");
                Environment.SetEnvironmentVariable("IISHWC_PID_FILE", pidFile);
                switch (args[0])
                {
                    case "start":
                        {
                            StartCartridge();
                            break;
                        }
                    case "stop":
                        {
                            StopCartridge();
                            break;
                        }
                    case "status":
                        {
                            CartridgeStatus();
                            break;
                        }
                    case "reload":
                        {
                            ReloadCartridge();
                            break;
                        }
                    case "restart":
                        {
                            RestartCartridge();
                            break;
                        }
                    case "build":
                        {
                            Build();
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
            return 0;
        }

        private static void StartCartridge()
        {
            if(ProcessRunning("cmd", pidFile))
            {
                Console.WriteLine("Cartridge already running");
                return;
            }
            Console.WriteLine("Startring the .NET cartridge");
            string logDir = Path.Combine(Environment.GetEnvironmentVariable("OPENSHIFT_DOTNET_DIR"), "log");
            Directory.CreateDirectory(logDir);
            ProcessStartInfo pi = new ProcessStartInfo();
            pi.WindowStyle = ProcessWindowStyle.Hidden;
            pi.FileName = "cmd";
            pi.Arguments = string.Format(@"/c {0}\bin\iishwc\start.bat  1>> {1}\stdout.log 2>> {1}\stderr.log", Environment.GetEnvironmentVariable("OPENSHIFT_DOTNET_DIR"), logDir);
            Process process = Process.Start(pi);
            Console.WriteLine(process.Id);
            File.WriteAllText(pidFile, process.Id.ToString());
        }

        private static void StopCartridge()
        {
            if(ProcessRunning("cmd", pidFile))
            {
                Console.WriteLine("Stopping");
                int processId = int.Parse(File.ReadAllText(pidFile));
                Process.Start("taskkill", string.Format("/F /T /PID {0}", processId)).WaitForExit();
                File.Delete(pidFile);
            }
            else
            {
                Console.WriteLine("Cartridge not running");
            }
        }

        private static void CartridgeStatus()
        {
            Console.WriteLine("Retrieving cartridge");
            if (ProcessRunning("cmd", pidFile))
            {
                ClientResult("Application is running");
            }
            else
            {
                ClientResult("Application is either stopped or inaccessible");
            }
        }

        private static void ReloadCartridge()
        {
            Console.WriteLine("Reloading cartridge");
            RestartCartridge();
        }

        private static void RestartCartridge()
        {
            Console.WriteLine("Restarting cartridge");
            StopCartridge();
            StartCartridge();
        }

        private static void Build()
        {
            ProcessStartInfo pi = new ProcessStartInfo();
            pi.WindowStyle = ProcessWindowStyle.Hidden;
            pi.FileName = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";
            pi.WorkingDirectory = Environment.GetEnvironmentVariable("OPENSHIFT_REPO_DIR");
            Process.Start(pi).WaitForExit();
        }

        private static bool ProcessRunning(string processName, string pidFile)
        {
            if(!File.Exists(pidFile))
            {
                return false;
            }

            int processId = int.Parse(File.ReadAllText(pidFile));
            Process process = Process.GetProcesses().Where(m => m.Id == processId && m.ProcessName == processName).FirstOrDefault();
            if(process == null)
            {
                return false;
            }
            return true;
        }

        private static void ClientResult(string text)
        {
            ClientOut("CLIENT_RESULT", text);
        }

        private static void ClientOut(string type, string output)
        {
            foreach(string line in output.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                string text = string.Format("{0}: {1}", type, line);
                Console.WriteLine(text);
            }
        }
    }
}
