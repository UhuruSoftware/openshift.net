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
            ProcessStartInfo shellStartInfo = new ProcessStartInfo();
            shellStartInfo.FileName = @"bash";
            shellStartInfo.UseShellExecute = false;
            
            Process shell = Process.Start(shellStartInfo);

            shell.WaitForExit();
        }
    }
}