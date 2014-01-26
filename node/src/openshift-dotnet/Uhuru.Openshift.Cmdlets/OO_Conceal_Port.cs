using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Conceal-Port")]
    public class OO_Conceal_Port : System.Management.Automation.Cmdlet 
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

        protected override void ProcessRecord()
        {
            ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName, WithContainerName,
                WithNamespace, null, null, null);

            try
            {
                this.WriteObject(container.DeletePublicEndpoints(this.CartName)); 
            }
            catch (Exception ex)
            {
                this.WriteObject(ex.ToString());
            }
        }
    }
}
