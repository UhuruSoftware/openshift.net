using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Utils;

namespace Uhuru.OpenShift.TrapUser
{
    class Program
    {
        static int Main(string[] args)
        {
            return UserShellTrap.StartShell();
        }
    }
}