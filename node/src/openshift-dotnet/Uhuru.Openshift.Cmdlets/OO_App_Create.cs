using System;
using System.Management.Automation;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "App-Create")]
    public class OO_App_Create : System.Management.Automation.Cmdlet 
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
        public SwitchParameter WithInitialDeploymentDir;

        [Parameter]
        public string WithRequestId;

        [Parameter]
        public string CartName;

        [Parameter]
        public string WithSecretToken;

        [Parameter]
        public string WithExposePorts;

        [Parameter]
        public int WithUid;

        [Parameter]
        public int WithQuotaBlocks;

        [Parameter]
        public int WithQuotaFiles;

        protected override void ProcessRecord()
        {            
            this.WriteObject(Execute());
        }

        public ReturnStatus Execute()
        {
            ReturnStatus status = new ReturnStatus();

            try
            {
                string token = null;
                if (!string.IsNullOrEmpty(WithSecretToken))
                    token = WithSecretToken;
                ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName,
                    WithContainerName, WithNamespace, null, null, null, WithUid);
                status.Output = container.Create(token);
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-app-create command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }
            return status;
        }
    }
}
