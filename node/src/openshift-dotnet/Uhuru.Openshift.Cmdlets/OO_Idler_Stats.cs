using System;
using System.IO;
using System.Management.Automation;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Config;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Idler-Stats")]
    public class OO_Idler_Stats : System.Management.Automation.Cmdlet
    {
        [Parameter]
        public SwitchParameter Validate;

        protected override void ProcessRecord()
        {
            this.WriteObject(Execute());
        }

        public ReturnStatus Execute()
        {
            ReturnStatus status = new ReturnStatus();
            try
            {
                string gearDir = NodeConfig.Values["GEAR_BASE_DIR"];
                int gearCount = Directory.GetDirectories(gearDir).Length;

                if (gearCount == 0)
                {
                    status.Output = "OK: No apps found. Nothing to monitor.";
                }
                else
                {
                    status.Output = String.Format("{0} running, 0 idled", gearCount);
                }
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-idler-start command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }
            return status;
        }
    }
}
