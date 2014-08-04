using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Control
{
    class Program
    {
        static string pidFile;
        static string instanceType;
        static string version;
        static int Main(string[] args)
        {
            try
            {
                version = ConfigurationManager.AppSettings["Version"];
                instanceType = ConfigurationManager.AppSettings["InstanceType"];

                pidFile = Path.Combine(Environment.GetEnvironmentVariable("OPENSHIFT_MSSQL_DIR"), "run", "mssql.pid");
                Environment.SetEnvironmentVariable("MSSQL_PID_FILE", pidFile);
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
                    default:
                        {
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
            return 0;
        }

        private static void StartCartridge()
        {
            if (ProcessRunning("cmd", pidFile))
            {
                Console.WriteLine("Cartridge already running");
                return;
            }
            Console.WriteLine(string.Format("Startring MSSQL {0} cartridge", version));
            string mssqlDir = Environment.GetEnvironmentVariable("OPENSHIFT_MSSQL_DIR");
            string logDir = Path.Combine(mssqlDir, "log");
            Directory.CreateDirectory(logDir);

            // set variables
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dbPort = Environment.GetEnvironmentVariable("OPENSHIFT_MSSQL_DB_PORT");
            string instanceName = string.Format("Instance{0}", dbPort);
            string instanceDir = Path.Combine(currentDir, string.Format("{0}.{1}", instanceType, instanceName));
            string dbName = Environment.GetEnvironmentVariable("OPENSHIFT_APP_NAME");
            string username = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            string password = File.ReadAllText(Path.Combine(instanceDir, "sqlpasswd"));

            File.WriteAllText(string.Format(@"{0}\env\OPENSHIFT_MSSQL_DB_USERNAME", mssqlDir), username);
            File.WriteAllText(string.Format(@"{0}\env\OPENSHIFT_MSSQL_DB_PASSWORD", mssqlDir), password);

            //build registry file
            string registryFile = Path.Combine(currentDir, "sqlserver.reg");
            WriteTemplate(Path.Combine(currentDir, string.Format(@"..\versions\{0}\sqlserver.reg.template", version)), registryFile, instanceName, currentDir, dbPort);

            //import registry file
            RunProcess(@"cmd.exe", "/C reg import " + registryFile + " /reg:64", "Error while importing registry file");

            //start SQL server service
            ProcessStartInfo sqlserver = new ProcessStartInfo();
            sqlserver.WindowStyle = ProcessWindowStyle.Hidden;
            sqlserver.FileName = @"cmd.exe";
            sqlserver.Arguments = string.Format(@"/c {0}\mssql\binn\sqlservr.exe -c -s {1} 1>>{2}\\stdout.log 2>>{2}\stderr.log", instanceDir, instanceName, logDir);
            Process sqlProcess = Process.Start(sqlserver);

            //create application database
            string connectionString = string.Format(@"server=127.0.0.1,{0}; database=master; User Id=sa; Password={1}; connection timeout=30", dbPort, password);
            SqlConnection sqlConnection = new SqlConnection(connectionString);

            bool success = false;

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Thread.Sleep(3000);
                    sqlConnection.Open();
                    SqlCommand sqlCmd = new SqlCommand(string.Format(@"IF NOT EXISTS(select * from sys.databases where name='{0}') CREATE DATABASE [{0}]", dbName));
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.ExecuteNonQuery();
                    success = true;
                    break;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.ToString());
                }   
                finally
                {
                    sqlConnection.Close();
                    sqlConnection.Dispose();
                }
            }

            if (!success)
            {
                throw new Exception("Cannot connect to SQL Server instance");
            }
           
            string text = string.Format("{0}Microsoft SQL Server {1} database added.  Please make note of these credentials:{0}{0}     sa password: {2}{0}   database name: {3}{0}{0}Connection URL: mssql://$OPENSHIFT_MSSQL_DB_HOST:$OPENSHIFT_MSSQL_DB_PORT/{0}",
                Environment.NewLine, version, password, dbName);
            ClientResult(text);

            Console.WriteLine(sqlProcess.Id);
            File.WriteAllText(pidFile, sqlProcess.Id.ToString());
        }

        private static void StopCartridge()
        {
            if (ProcessRunning("cmd", pidFile))
            {
                Console.WriteLine("Stopping");
                int processId = int.Parse(File.ReadAllText(pidFile));
                Process.Start("taskkill", string.Format("/F /T /PID {0}", processId)).WaitForExit();
                File.Delete(pidFile);
            }
            else
            {
                Console.WriteLine("Cartridge is not running");
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

        private static bool ProcessRunning(string processName, string pidFile)
        {
            if (!File.Exists(pidFile))
            {
                return false;
            }

            int processId = int.Parse(File.ReadAllText(pidFile));
            Process process = Process.GetProcesses().Where(m => m.Id == processId && m.ProcessName == processName).FirstOrDefault();
            if (process == null)
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
            foreach (string line in output.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                string text = string.Format("{0}: {1}", type, line);
                Console.WriteLine(text);
            }
        }

        private static void WriteTemplate(string inFile, string outFile, string instanceName, string baseDir, string tcpPort)
        {
            baseDir = baseDir.Replace(@"\", @"\\");
            string content = File.ReadAllText(inFile, System.Text.Encoding.ASCII).Replace("${InstanceName}", instanceName).Replace("${BaseDir}", baseDir).Replace("${tcpPort}", tcpPort);
            File.WriteAllText(outFile, content, System.Text.Encoding.ASCII);
        }

        private static void RunProcess(string processFile, string arguments, string exception)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.FileName = processFile;
            processInfo.Arguments = arguments;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;
            Process process = Process.Start(processInfo);
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            if (process.ExitCode != 0)
            {
                throw new Exception(string.Format("{0}: {1}", exception, process.StandardError.ReadToEnd()));    
            }
        }
    }
}
