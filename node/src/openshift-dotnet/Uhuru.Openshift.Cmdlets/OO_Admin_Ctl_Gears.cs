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
        [Parameter]
        public string Operation;

        [Parameter]
        public decimal UUID;

        protected override void ProcessRecord()
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                this.WriteObject(ex.ToString());
            }
        }
    }
}
