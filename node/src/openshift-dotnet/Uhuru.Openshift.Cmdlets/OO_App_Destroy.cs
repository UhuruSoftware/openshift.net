using System;
using System.Management.Automation;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "App-Destroy")]
    public class OO_App_Destroy : System.Management.Automation.Cmdlet
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
        public string CartName;

        [Parameter]
        public string WithExposePorts;

        [Parameter]
        public int WithUid;

        [Parameter]
        public bool SkipHooks;


        protected override void ProcessRecord()
        {            
            this.WriteObject(this.Execute());
        }

        public ReturnStatus Execute()
        {
            ReturnStatus status = new ReturnStatus();
            try
            {                
                ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName, WithContainerName,
                   WithNamespace, null, null, null, WithUid);
                status.Output = container.Destroy();
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-app-destroy command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }
            return status;
        }
    }
}
