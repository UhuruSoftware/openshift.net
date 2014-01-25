using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Utilities
{
    public class ProcessResult
    {
        public string StdOut
        {
            get;
            set;
        }

        public string StdErr
        {
            get;
            set;
        }

        public int ExitCode
        {
            get;
            set;
        }
    }
}
