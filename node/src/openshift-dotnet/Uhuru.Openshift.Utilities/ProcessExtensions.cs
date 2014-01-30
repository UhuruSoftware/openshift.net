using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;

namespace Uhuru.Openshift.Utilities
{
    public static class ProcessExtensions
    {
        public static void KillProcessAndChildren(this Process process)
        {
            KillProcessAndChildren(process.Id);
        }

        public static string Get64BitPowershell()
        {
            string path = "c:\\windows\\sysnative\\windowspowershell\\v1.0\\powershell.exe";
            
            return File.Exists(path) ? path : "powershell.exe";
        }

        public static void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        public static ProcessResult RunCommandAndGetOutput(string command, string arguments, string workingDirectory = null)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = command;
            start.Arguments = arguments;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            if (workingDirectory != null)
            {
                start.WorkingDirectory = workingDirectory;
            }
            using (Process process = Process.Start(start))
            {
                string result = process.StandardOutput.ReadToEnd();
                string resultError = process.StandardError.ReadToEnd();

                process.WaitForExit();

                return new ProcessResult()
                {
                    ExitCode = process.ExitCode,
                    StdErr = resultError,
                    StdOut = result
                };
            }
        }
    }
}
