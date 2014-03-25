using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Text;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Common.OODiagnostics.Tests
{
    public class TestScheduledTasks: ITest
    {

        ExitCode exitCode = ExitCode.PASS;
        string[] TASKS = new string[] { "openshift.facts", "openshift.startup" };

        public string GetName()
        {
            return "test_scheduled_tasks";
        }

        public void Run()
        {
            Output.WriteDebug("Checking that required scheduled tasks exist");
            
            List<string> notFoundTasks = new List<string>();
            
           

            foreach (string task in TASKS)
            {
                try
                {
                    ProcessResult result = ProcessExtensions.RunCommandAndGetOutput("PowerShell.exe", string.Format("Get-ScheduledTask -Taskname {0}", task), null);
                    if (result.StdErr.Trim() != string.Empty)
                    {
                        notFoundTasks.Add(task);
                    }
                }
                catch (Win32Exception)
                {
                    notFoundTasks.Add(task);
                }
                
            }

            if (notFoundTasks.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("The following task(s) are missing:");
                sb.AppendLine(String.Join(", ", notFoundTasks.ToArray()));
                sb.AppendLine("These tasks are required for OpenShift functionality.");
                Output.WriteFail(sb.ToString());
                exitCode = ExitCode.FAIL;
            }
            
            
        }

        public ExitCode GetExitCode()
        {
            return exitCode;
        }

        


    }
}
