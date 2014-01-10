using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime
{
    public partial class ApplicationContainer
    {
        public string Deconfigure(string cartName)
        {
            return this.Cartridge.Deconfigure(cartName);
        }
    }
}
