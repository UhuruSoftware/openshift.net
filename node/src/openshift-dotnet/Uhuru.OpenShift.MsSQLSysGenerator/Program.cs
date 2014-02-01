using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uhuru.Openshift.MsSQLSysGenerator
{
    class Program
    {
        static string CommandFormat = "MsSQLSysGenerator.exe pass=currentSAPassword dir=destinationDirectory newPass=newSAPassword";
        static string currentPass = string.Empty;
        static string destinationDir = string.Empty;
        static string newPass = string.Empty;
        static string mssqlBasePath = string.Empty;
        static string mssqlRegPath = string.Empty;
        static string mssqlDefaultInstanceName = "MSSQLSERVER";
        static string workingDirectory = string.Empty;
        static string backupDirectory = string.Empty;

        static int Main(string[] args)
        {
            if (!ValidArgs(args))
            {
                return -1;
            }

            currentPass = args[0].Split('=')[1];
            destinationDir = args[1].Split('=')[1];
            newPass = args[2].Split('=')[1];
            
            mssqlBasePath = (string)Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL10_50.MSSQLSERVER\Setup").GetValue("SQLPath");
            mssqlRegPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL10_50.MSSQLSERVER";
            workingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            backupDirectory = Path.Combine(workingDirectory, "sql_instance_backup");
            
            foreach (Process p in Process.GetProcessesByName("sqlservr"))
            {
                if (p.MainModule.FileName.ToLower() == Path.Combine(mssqlBasePath, "Binn", "sqlservr.exe").ToLower())
                {
                    Output.WriteInfo(string.Format("Stopping SQL Server with pid {0}", p.Id));
                    p.Kill();
                }
            }

            BackupSystem();
            Process defaultInstance = StartMSSQLInstance("MSSQLSERVER", mssqlBasePath);

            CheckMSSQLInstance("", currentPass);

            ChangeDatabaseLocations();

            ChangeAdminPassword();

            Output.WriteInfo("Stopping SQL Server");
            defaultInstance.Kill();

            Output.WriteInfo("Copying files to new location");
            FileHelper fileHelper = new FileHelper();
            fileHelper.Copy1(mssqlBasePath, destinationDir);
            Output.WriteSuccess("Done");

            Output.WriteInfo("Restoring databases to default instance");
            fileHelper = new FileHelper();
            fileHelper.Copy1(Path.Combine(backupDirectory, "MSSQLServer"), Path.Combine(mssqlBasePath, "DATA"));
            Output.WriteSuccess("Done");

            Output.WriteInfo("Cleaning up backup");
            Directory.Delete(backupDirectory, true);

            
            return 0;

        }

        static void ChangeAdminPassword()
        {
            Output.WriteInfo("Changing SA user passwrord");

            string connectionString = GetConnectionString(currentPass);
            SqlConnection sqlConnection = new SqlConnection(connectionString);
            try
            {
                sqlConnection.Open();
                SqlCommand sqlCmd = new SqlCommand();
                sqlCmd.CommandText = string.Format(@"alter login [sa] with password = '{0}'", newPass);
                sqlCmd.Connection = sqlConnection;
                sqlCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Output.WriteError(ex.ToString());
                throw ex;
            }
            finally
            {
                sqlConnection.Close();
                sqlConnection.Dispose();
            }
            Output.WriteSuccess("Success!");
        }

        static void ChangeDatabaseLocations()
        {
            Output.WriteInfo("Changing database location");

            string connectionString = GetConnectionString(currentPass);
            SqlConnection sqlConnection = new SqlConnection(connectionString);
            SqlDataReader sqlReader = null;
            try
            {
                string newDataDir = Path.Combine(destinationDir, "DATA");
                if (!Directory.Exists(newDataDir))
                {
                    Directory.CreateDirectory(newDataDir);
                }
                sqlConnection.Open();
                SqlCommand sqlCmd = new SqlCommand();
                sqlCmd.Connection = sqlConnection;

                Output.WriteInfo("Retrieving system database info");
                sqlCmd.CommandText = "SELECT physical_name,name, (select name from sys.databases where database_id = sys.master_files.database_id) as dbName FROM sys.master_files";

                sqlReader = sqlCmd.ExecuteReader();

                List<string> commands = new List<string>();

                while (sqlReader.Read())
                {
                    string newFilePath = Path.Combine(newDataDir, Path.GetFileName((string)sqlReader["physical_name"]));
                    commands.Add(string.Format(@"ALTER DATABASE {0} MODIFY FILE (NAME = {1}, FILENAME = '{2}')", (string)sqlReader["dbName"], (string)sqlReader["name"], newFilePath));
                }

                sqlReader.Close();
                sqlReader.Dispose();

                Output.WriteInfo("Retrieving system database info");
                foreach (string command in commands)
                {
                    sqlCmd.CommandText = command;
                    sqlCmd.ExecuteNonQuery();
                }

                Output.WriteSuccess("Success!");


            }
            catch (Exception ex)
            {
                Output.WriteError(ex.ToString());
                throw ex;
            }
            finally
            {
                if (sqlReader != null)
                {
                    if (!sqlReader.IsClosed)
                    {
                        sqlReader.Close();
                        sqlReader.Dispose();
                    }
                }
                sqlConnection.Close();
                sqlConnection.Dispose();
                
            }

        }

        static Process StartMSSQLInstance(string instanceName, string path)
        {
            Output.WriteInfo(string.Format("Starting MSSQL instance {0}", instanceName));
            Process proc = new Process();
            try
            {
                proc.StartInfo.FileName = "sqlservr.exe";
                proc.StartInfo.UseShellExecute = false;
                
                proc = Process.Start(Path.Combine(path, "Binn", "sqlservr.exe"), "-c -s " + instanceName);
            }
                
            catch (Exception ex)
            {
                Output.WriteError(ex.ToString());
                throw ex;
            }
            return proc;
        }

        static void CheckMSSQLInstance(string instanceName, string password)
        {
            Output.WriteInfo(string.Format("Checking instance {0}", (instanceName != string.Empty ? "Default Instance": instanceName)));
            string connectionString = GetConnectionString(instanceName, password);
            SqlConnection sqlConnection = new SqlConnection(connectionString);

            bool success = false;

            for (int i = 0; i < 10; i++)
            {
                Output.WriteWarning(string.Format("Try {0} of 10 to connect to SQL server", i+1));
                try
                {
                    Thread.Sleep(3000);
                    sqlConnection.Open();
                    success = true;
                    Output.WriteSuccess("Success");
                    break;
                }
                catch (Exception ex)
                {
                    Output.WriteError(ex.ToString());
                }
            }
            if (!success)
            {
                throw new Exception("Cannot connect to SQL Server instance");
            }
        }

        static string GetConnectionString(string password)
        {
            return GetConnectionString(string.Empty, password);
        }

        static string GetConnectionString(string instanceName, string password)
        {
            string connectionString = string.Format(@"user id=sa; password={0}; server=127.0.0.1\{1}; database=master; connection timeout=30", password, instanceName);
            return connectionString;
        }

        static void BackupSystem()
        {
            Output.WriteInfo("Backing up system");

            //Output.WriteInfo("Stopping default instance");
            //ServiceController sc = new ServiceController(mssqlDefaultInstanceName);
            //if (sc.Status != ServiceControllerStatus.Stopped)
            //{
            //    sc.Stop();
            //}
            //while ((new ServiceController(mssqlDefaultInstanceName)).Status != ServiceControllerStatus.Stopped)
            //{
            //    Output.WriteInfo("Waiting For SQL Server To stop.");
            //    Thread.Sleep(2000);
            //}
            //Output.WriteSuccess("Done");

            //Output.WriteInfo("Backing up registry");
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }
         
            //RegUtil.ExportSqlKey(Path.Combine(backupDirectory, "existing.reg"), mssqlRegPath);
            CopyDefaultInstance();

            Output.WriteSuccess("Backup Done!");
        }

        static void CopyDefaultInstance()
        {
            Output.WriteInfo("Backing up default databases");
            FileHelper fileHelper = new FileHelper();
            fileHelper.Copy1(Path.Combine(mssqlBasePath, "DATA"), Path.Combine(backupDirectory, "MSSQLServer"));
            Output.WriteSuccess("Done!");
            
        }


        static bool ValidArgs(string[] args)
        {
            if (args.Length != 3)
            {
                Output.WriteError("Invalid number of parameters. The comand format is :" + Environment.NewLine + CommandFormat);
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Split('=').Length != 2)
                {
                    Output.WriteError(string.Format("Invalid parameter at position {0}", i));
                    return false;
                }
            }

            bool validPath = true;
            foreach (var c in args[1].Split('=')[1].Where(Path.GetInvalidPathChars().Contains))
            {
                Output.WriteError(string.Format("Provided path contains invalid character {0}", c));
                validPath = false;
            }
            if (!validPath)
            {
                return false;
            }

            return true;
        }
    }
}
