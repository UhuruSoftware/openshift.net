using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
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
        public string ComponentName;

        [Parameter]
        public string WithSoftwareVersion;

        [Parameter]
        public string CartridgeVendor;

        [Parameter]
        public string WithKey;

        [Parameter]
        public string WithValue;

        protected override void ProcessRecord()
        {
            ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName,
               WithContainerName, WithNamespace, null, null, null);
            ReturnStatus status = new ReturnStatus();
            try
            {
                container.AddEnvVar(WithKey, WithValue);
                status.Output = string.Empty;
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                status.Output = ex.ToString();
                status.ExitCode = -1;
            }
            this.WriteObject(status);            
        }
    }
}
