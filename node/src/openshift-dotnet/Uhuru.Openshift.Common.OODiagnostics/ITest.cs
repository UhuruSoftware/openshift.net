using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Common.OODiagnostics
{
    public enum ExitCode
    {
        PASS,
        WARNING,
        FAIL
    }

    interface ITest
    {
        string GetName();

        void Run();

        ExitCode GetExitCode();

    }
}
