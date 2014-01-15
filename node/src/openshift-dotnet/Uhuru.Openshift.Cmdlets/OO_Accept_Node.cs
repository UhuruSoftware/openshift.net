using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Accept-Node")]
    public class OO_Accept_Node : System.Management.Automation.Cmdlet
    {
        [Parameter]
        public bool Verbose;

        [Parameter]
        public decimal Timeout;

        [Parameter]
        public bool RunUpgradeChecks;

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
