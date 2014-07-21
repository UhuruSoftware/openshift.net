using System;
using System.Management.Automation;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Tidy")]
    public class OO_Tidy : System.Management.Automation.Cmdlet 
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
        public string ComponentName;

        [Parameter]
        public string WithSoftwareVersion;

        [Parameter]
        public string CartridgeVendor;

        [Parameter]
        public string WithExposePorts;

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
                ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName,
                            WithContainerName, WithNamespace, null, null, null, WithUid);

                status.Output = container.Tidy();
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-tidy command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }
            return status;
        }
    }
}
