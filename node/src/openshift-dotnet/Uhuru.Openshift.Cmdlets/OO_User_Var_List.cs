using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "User-Var-List")]
    public class OO_User_Var_List : System.Management.Automation.Cmdlet 
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
        public string WithKeys;

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

            ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName,
                WithContainerName, WithNamespace, null, null, null, WithUid);
            try
            {
                string output = string.Empty;
                string[] keys = null;
                if (!string.IsNullOrEmpty(WithKeys))
                {
                    keys = WithKeys.Split(' ');
                }

                Dictionary<string, string> variables = container.UserVarList(keys);

                status.Output = string.Format("{0}CLIENT_RESULT: {1}{0}", Environment.NewLine, JsonConvert.SerializeObject(variables));
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-user-var-list command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }
            return status;
        }
    }
}
