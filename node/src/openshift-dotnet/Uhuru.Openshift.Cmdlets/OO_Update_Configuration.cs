using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Update-Configuration")]
    public class OO_Update_Configuration : System.Management.Automation.Cmdlet
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
        public bool AutoDeploy;

        [Parameter]
        public string DeploymentBranch;

        [Parameter]
        public int KeepDeployments;

        [Parameter]
        public string DeploymentType;

        protected override void ProcessRecord()
        {
            ReturnStatus status = new ReturnStatus();
            try
            {
                ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName,
                    WithContainerName, WithNamespace, null, null, null);
                container.SetAutoDeploy(AutoDeploy);
                container.SetDeploymentBranch(DeploymentBranch);
                container.SetKeepDeployments(KeepDeployments);
                container.SetDeploymentType(DeploymentType);
                status.ExitCode = 0;
                status.Output = string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-update-configuration command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }
            this.WriteObject(status);
        }
    }
}
