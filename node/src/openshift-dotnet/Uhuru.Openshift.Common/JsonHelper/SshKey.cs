using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Common.JsonHelper
{
    public class SshKey
    {
        public string Comment { get; set; }
        public string Key { get; set; }
        public string Type { get; set; }
    }
}
