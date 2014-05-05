using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime;
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

                string homeDir = Environment.GetEnvironmentVariable("HOME");

                UserShellTrap.SetupGearEnv(envVars, homeDir);

                string userHomeDir = envVars.ContainsKey("OPENSHIFT_HOMEDIR") && Directory.Exists(envVars["OPENSHIFT_HOMEDIR"]) ? envVars["OPENSHIFT_HOMEDIR"] : string.Empty;

                var prison = Prison.Prison.LoadPrisonAndAttach(Guid.Parse(Uuid.PadLeft(32, '0')));
                
                UserShellTrap.FixHomeDir(userHomeDir, prison.User.Username, Uuid);
                 
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
