using System;
using System.Management.Automation;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Restart")]
    public class OO_Restart : System.Management.Automation.Cmdlet 
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
        public SwitchParameter All;

        [Parameter]
        public string WithExposePorts;

        [Parameter]
        public float ParallelConcurrencyRatio;

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
                RubyHash options = new RubyHash();
                options["all"] = All;
                if (ParallelConcurrencyRatio != 0.0)
                {
                    options["parallelConcurrencyRatio"] = ParallelConcurrencyRatio;
                }

                status.Output = container.Restart(CartName, options);
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-restart command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;

            }
            return status;
        }        
    }
}
