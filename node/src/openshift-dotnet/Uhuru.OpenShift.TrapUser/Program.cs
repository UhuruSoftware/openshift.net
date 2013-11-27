using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.OpenShift.TrapUser
{
    class Program
    {
        static void Main(string[] args)
        {
            UserShellTrap.SetupGearEnv();
            UserShellTrap.StartShell();
        }
    }
}