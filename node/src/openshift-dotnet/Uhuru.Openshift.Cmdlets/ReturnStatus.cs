using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Cmdlets
{
    public class ReturnStatus
    {
        public int ExitCode { get; set; }
        public object Output { get; set; }

        public ReturnStatus() { }

        public ReturnStatus(object output, int exitCode)
        {
            this.ExitCode = exitCode;
            this.Output = output;
        }
    }
}
