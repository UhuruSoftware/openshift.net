using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Uhuru.Openshift.Common.JsonHelper;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "User-Var-Add")]
    public class OO_User_Var_Add : System.Management.Automation.Cmdlet 
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
        public string WithVariables;

        [Parameter]
        public string WithGears;

        protected override void ProcessRecord()
        {
            ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName,
               WithContainerName, WithNamespace, null, null, null);

            try
            {
                JArray varsArray = (JArray)JsonConvert.DeserializeObject(WithVariables);
                List<NameValuePair> vars = varsArray.ToObject<List<NameValuePair>>();

                Dictionary<string, string> variables = new Dictionary<string, string>();
                List<string> gears = null;

                foreach (var varObj in vars)
                {
                    variables.Add(varObj.Name, varObj.Value);
                }

                if (!string.IsNullOrEmpty(WithGears))
                {
                    gears = new List<string>();
                    JArray gearsObj = (JArray)JsonConvert.DeserializeObject(WithGears);
                    foreach (var gearObj in gearsObj)
                    {
                        gears.Add(gearObj.ToString());
                    }
                }
               
                WriteObject(container.AddUserVar(variables, gears));
            }
            catch (Exception ex)
            {
                this.WriteObject(ex.ToString());
            }
        }

    }
}
