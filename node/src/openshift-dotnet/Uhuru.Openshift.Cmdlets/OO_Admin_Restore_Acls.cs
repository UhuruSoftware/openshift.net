using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Utilities;
using Uhuru.OpenShift.TrapUser;


namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Admin-Restore-Acls")]
    public class OO_Admin_Restore_Acls : System.Management.Automation.Cmdlet
    {
        [Parameter]
        public string Uuid;

        protected override void ProcessRecord()
        {
            this.WriteObject(Execute());
        }

        public ReturnStatus Execute()
        {
            ReturnStatus status = new ReturnStatus();

            try
            {                
                Dictionary<string, string> envVars = new Dictionary<string, string>();

                //string homeDir = Environment.GetEnvironmentVariable("HOME");
                string homeDir = Path.Combine(@"C:\openshift\gears", Uuid);

                UserShellTrap.SetupGearEnv(envVars, homeDir);

                string userHomeDir = envVars.ContainsKey("OPENSHIFT_HOMEDIR") && Directory.Exists(envVars["OPENSHIFT_HOMEDIR"]) ? envVars["OPENSHIFT_HOMEDIR"] : string.Empty;

                var prison = Prison.Prison.LoadPrisonAndAttach(PrisonIdConverter.Generate(Uuid));
                
                UserShellTrap.FixHomeDir(userHomeDir, prison.User.Username, Uuid);
                
                if (Directory.Exists(Path.Combine(homeDir, "mssql")))
                {                  
                  string[] instancefolderinfo=Directory.GetDirectories(Path.Combine(homeDir,"mssql","bin")).First().Split('.');
                  instancefolderinfo[0] = instancefolderinfo[0].Substring(instancefolderinfo[0].LastIndexOf('\\')+1);
                  Logger.Info("Reconfiguring registry after move with parameters {0},{1} for user {2}", instancefolderinfo[0], instancefolderinfo[1], prison.User.Username);
                  switch (instancefolderinfo[0]) {
                      case "MSSQL11": { Prison.MsSqlInstanceTool.ConfigureMsSqlInstanceRegistry(prison, instancefolderinfo[0], "MSSQLSERVER2012"); break; }
                      case "MSSQL10_50": { Prison.MsSqlInstanceTool.ConfigureMsSqlInstanceRegistry(prison, instancefolderinfo[0], "MSSQLSERVER"); break; }
                      default:{throw new Exception("Unsupported MSSQL version!");}
                  }

                  foreach (string file in Directory.GetFiles(Path.Combine(homeDir, "mssql", "bin",instancefolderinfo[0]+"."+instancefolderinfo[1],"mssql","DATA")))
                  {
                      FileSecurity fSecurity = File.GetAccessControl(file);
                      fSecurity.AddAccessRule(new FileSystemAccessRule(prison.User.Username, FileSystemRights.FullControl
                          , AccessControlType.Allow));
                      File.SetAccessControl(file, fSecurity);                      
                  }
                }                
                 
                status.ExitCode = 0;                
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-admin-restore-acls command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }

            return status;
        }
    }
}
