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
            string output = Node.GetCartridgeList(WithDescriptors, Porcelain, false);
            this.WriteObject(output);
        }
    }
}
