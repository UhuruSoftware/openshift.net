using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Trap-User")]
    public class OO_Trap_User : System.Management.Automation.Cmdlet 
    {
        protected override void ProcessRecord()
        {
            UserShellTrap userShellTrap = new UserShellTrap();
        }
    }
}
