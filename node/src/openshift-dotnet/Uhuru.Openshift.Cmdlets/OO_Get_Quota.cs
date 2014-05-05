using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO","Get-Quota")]
    public class OO_Get_Quota : System.Management.Automation.Cmdlet
    {
        [Parameter]
        public string Uuid;

        [Parameter]
        public string CartName;

        protected override void ProcessRecord()
        {
            this.WriteObject(Execute());
        }

        public ReturnStatus Execute()
        {           
            ReturnStatus status = new ReturnStatus();

            try
            {                
                ApplicationContainer container = ApplicationContainer.GetFromUuid(Uuid);
                status.Output = container.GetQuota(CartName);                           
                status.ExitCode = 0;
            }
            catch(Exception ex)
            {
                Logger.Error("Error running oo-get-quota command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }

            return status;
        }
    }
}
