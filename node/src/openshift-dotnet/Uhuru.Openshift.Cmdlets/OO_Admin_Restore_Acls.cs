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
            for (int attempts = 0; attempts < 3; attempts++)
            {
                try
                {
                    Logger.Info("Running oo-admin-restore-acls for gear {0}", this.Uuid);
                    Dictionary<string, string> envVars = new Dictionary<string, string>();

                    string homeDir = @"c:\openshift\gears\";

                    homeDir = Path.Combine(homeDir, Uuid);

                    UserShellTrap.SetupGearEnv(envVars, homeDir);

                    string userHomeDir = envVars.ContainsKey("OPENSHIFT_HOMEDIR") && Directory.Exists(envVars["OPENSHIFT_HOMEDIR"]) ? envVars["OPENSHIFT_HOMEDIR"] : string.Empty;

                    var prison = Prison.Prison.LoadPrisonAndAttach(Guid.Parse(Uuid.PadLeft(32, '0')));

                    UserShellTrap.FixHomeDir(userHomeDir, prison.User.Username, Uuid);

                    Logger.Info("Resetting permisions for ssh authorized_keys");
                    string sshDir = Path.Combine(homeDir, ".ssh");                    
                    string authorizedKeys = Path.Combine(sshDir, "authorized_keys");

                    string keyFileLinux = Uhuru.Openshift.Runtime.Utils.LinuxFiles.Cygpath(authorizedKeys);
                    string sshDirLinux = Uhuru.Openshift.Runtime.Utils.LinuxFiles.Cygpath(sshDir);
                    string homeDirLinux = Uhuru.Openshift.Runtime.Utils.LinuxFiles.Cygpath(homeDir);

                    Uhuru.Openshift.Runtime.Utils.LinuxFiles.Chmod(homeDirLinux, "700");
                    Uhuru.Openshift.Runtime.Utils.LinuxFiles.Chmod(sshDirLinux, "600");
                    Uhuru.Openshift.Runtime.Utils.LinuxFiles.Chmod(keyFileLinux, "600");
                                        
                    Uhuru.Openshift.Runtime.Utils.LinuxFiles.Chown(Uuid,keyFileLinux);
                    
                                       
                    status.Output = string.Format("User rights were reset for gear:{0}", Uuid);
                    status.ExitCode = 0;
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error running oo-admin-restore-acls command on attempt {0}: {1} - {2}",attempts+1, ex.Message, ex.StackTrace);
                    status.Output = ex.ToString();
                    status.ExitCode = 1;
                }
                System.Threading.Thread.Sleep(1000);
            }

            return status;
        }
    }
}
