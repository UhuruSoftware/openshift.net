using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("Has", "App-Cartridge-Action")]
    public class Has_App_Cartridge_Action : System.Management.Automation.Cmdlet 
    {
        [Parameter]
        public string AppUuid;

        [Parameter]
        public string GearUuid;

        [Parameter]
        public string CartName;

        protected override void ProcessRecord()
        {
            ReturnStatus returnStatus = new ReturnStatus();
            try
            {
                ApplicationContainer container = ApplicationContainer.GetFromUuid(GearUuid);
                Manifest cartridge = container.GetCartridge(CartName);
                if (cartridge != null)
                {
                    returnStatus.Output = "true";
                    returnStatus.ExitCode = 0;
                }
                else
                {
                    returnStatus.Output = "false";
                    returnStatus.ExitCode = 1;
                }
            }
            catch (Exception ex)
            {
                //TODO logging
                returnStatus.Output = "false";
                returnStatus.ExitCode = 1;
            }

            this.WriteObject(returnStatus);
        }
    }
}
