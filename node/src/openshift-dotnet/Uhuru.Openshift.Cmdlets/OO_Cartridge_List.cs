using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using Uhuru.Openshift.Common;
using Uhuru.Openshift.Common.Models;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Cartridge-List")]
    public class OO_Cartridge_List : System.Management.Automation.Cmdlet 
    {
        [Parameter]
        public bool Porcelain;

        [Parameter]
        public bool WithDescriptors;

        [Parameter]
        public string CartName;

        protected override void ProcessRecord()
        {
            ReturnStatus status = new ReturnStatus();
            try
            {
                status.Output = Node.GetCartridgeList(WithDescriptors, Porcelain, false);                
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-cartridge-list command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }
            this.WriteObject(status);
        }
    }
}
