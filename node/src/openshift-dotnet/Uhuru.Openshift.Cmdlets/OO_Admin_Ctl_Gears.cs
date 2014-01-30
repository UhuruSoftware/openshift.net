using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Admin-Ctl-Gears")]
    public class OO_Admin_Ctl_Gears : System.Management.Automation.Cmdlet
    {
        [Parameter(Position = 1, Mandatory = true)]
        [ValidateSet("startall", "stopall", "forcestopall", "status", "restartall", "waited-startall", "condrestartall", "startgear")]
        public string Operation;

        [Parameter(Position=2)]
        public decimal UUID;

        protected override void ProcessRecord()
        {
            try
            {
                // TODO
            }
            catch (Exception ex)
            {
                this.WriteObject(ex.ToString());
            }
        }
    }
}
