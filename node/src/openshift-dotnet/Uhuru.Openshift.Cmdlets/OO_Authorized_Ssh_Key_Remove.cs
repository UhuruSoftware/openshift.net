using System;
using System.Management.Automation;
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

        [Parameter]
        public int WithUid;

        protected override void ProcessRecord()
        {                    
            this.WriteObject(Execute());
        }

        public ReturnStatus Execute()
        {
            ReturnStatus status = new ReturnStatus();
            try
            {
                ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName, WithContainerName,
                    WithNamespace, null, null, null, WithUid);
                status.Output = container.RemoveSshKey(WithSshKey, WithSshKeyType, WithSshComment);
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-authorized-ssh-key-remove command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }
            return status;
        }
    }
}
