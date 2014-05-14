using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Utilities
{
    public class SessionUtil
    {

        public static void ValidateSessionForWindowsIsolation()
        {
            int currentSession = GetCurrentSessionId();

            if (currentSession != 0)
            {
                throw new Exception("Windows Isolation operations must be executed from Session 0. Use an SSH shell, Task Scheduler, or PSExec to run a process in Session 0.");
            }
        }

        public static int GetCurrentSessionId()
        {
            return Process.GetCurrentProcess().SessionId;
        }

    }
}
