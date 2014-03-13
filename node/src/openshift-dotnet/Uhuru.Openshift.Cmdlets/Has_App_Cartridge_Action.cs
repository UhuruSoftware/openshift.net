using System;
using System.Management.Automation;
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
            this.WriteObject(Execute());
        }

        public ReturnStatus Execute()
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
                Logger.Error("Error running has-app-cartridge-action command: {0} - {1}", ex.Message, ex.StackTrace);
                returnStatus.Output = "false";
                returnStatus.ExitCode = 1;
            }
            return returnStatus;
        }
    }
}
