using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
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
            try
            {
                string gearDir = NodeConfig.Values["GEAR_BASE_DIR"];
                int gearCount = Directory.GetDirectories(gearDir).Length;

                if (gearCount == 0)
                {
                    WriteObject("OK: No apps found. Nothing to monitor.");
                }
                else
                {
                    WriteObject(String.Format("{0} running, 0 idled", gearCount));
                }
            }
            catch (Exception ex)
            {
                this.WriteObject(ex.ToString());
            }
        }
    }
}
