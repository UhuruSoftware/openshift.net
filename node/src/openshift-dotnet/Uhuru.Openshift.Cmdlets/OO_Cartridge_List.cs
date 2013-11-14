using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using Uhuru.Openshift.Cmdlets.ClassLibrary;
using Uhuru.Openshift.Cmdlets.ClassLibrary.Models;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "CartridgeList")]
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
            string output = ClassLibrary.Node.GetCartridgeList(WithDescriptors, Porcelain, false);
            this.WriteObject(output);
        }
    }
}
