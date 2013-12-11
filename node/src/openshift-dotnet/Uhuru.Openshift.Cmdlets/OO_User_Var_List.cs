using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
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

        protected override void ProcessRecord()
        {
            ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName,
                WithContainerName, WithNamespace, null, null, null);
            try
            {
                string output = string.Empty;
                string[] keys = null;
                if (!string.IsNullOrEmpty(WithKeys))
                {
                    keys = WithKeys.Split(' ');
                }

                Dictionary<string, string> variables = container.UserVarList(keys);
                
                this.WriteObject(string.Format("{0}CLIENT_RESULT: {1}{0}", Environment.NewLine, JsonConvert.SerializeObject(variables)));

            }
            catch (Exception ex)
            {
                this.WriteObject(ex.ToString());
            }
        }
    }
}
