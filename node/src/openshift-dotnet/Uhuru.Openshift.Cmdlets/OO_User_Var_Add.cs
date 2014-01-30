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
            ReturnStatus status = new ReturnStatus();

            ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName,
               WithContainerName, WithNamespace, null, null, null);

            try
            {
                Dictionary<string, string> variables = new Dictionary<string, string>();

                if (!string.IsNullOrWhiteSpace(WithVariables))
                {
                    foreach (string variable in WithVariables.Trim().Split(' '))
                    {
                        variables.Add(variable.Split('=')[0].Trim(), variable.Split('=')[1].Trim());
                    }
                    
                }

                List<string> gears = new List<string>();

                if (!string.IsNullOrEmpty(WithGears))
                {
                    gears = this.WithGears.Split(';').ToList();
                }
          
                status.Output = container.AddUserVar(variables, gears);
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-user-var-add command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }
            this.WriteObject(status);
        }

    }
}
