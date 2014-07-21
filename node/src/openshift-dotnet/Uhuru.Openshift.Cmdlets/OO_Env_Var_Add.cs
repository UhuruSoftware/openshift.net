using System;
using System.Management.Automation;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Env-Var-Add")]
    public class OO_Env_Var_Add : System.Management.Automation.Cmdlet
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
        public string WithKey;

        [Parameter]
        public string WithValue;

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
            
                container.AddEnvVar(WithKey, WithValue);
                status.Output = string.Empty;
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-env-var-add command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = -1;
            }
            return status;
        }
    }
}
