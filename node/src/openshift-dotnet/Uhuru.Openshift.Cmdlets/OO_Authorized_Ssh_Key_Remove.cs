using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Authorized-Ssh-Key-Remove")]
    public class OO_Authorized_Ssh_Key_Remove : System.Management.Automation.Cmdlet 
    {
        [Parameter]
        public string WithAppUuid;

        [Parameter]
        public string WithAppName;

        [Parameter]
        public string WithContainerUuid;

        [Parameter]
        public string WithContainerName;

        [Parameter]
        public string WithNamespace;

        [Parameter]
        public string WithRequestId;

        [Parameter]
        public string WithSshKey;

        [Parameter]
        public string WithSshKeyType;

        [Parameter]
        public string WithSshComment;

        protected override void ProcessRecord()
        {        
            ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName, WithContainerName,
                WithNamespace, null, null, null);

            ReturnStatus status = new ReturnStatus();
            try
            {
                status.Output = container.RemoveSshKey(WithSshKey, WithSshKeyType, WithSshComment);
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }
            this.WriteObject(status);
        }
    }
}
