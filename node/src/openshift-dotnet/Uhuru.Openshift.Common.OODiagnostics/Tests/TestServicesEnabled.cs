using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Common.OODiagnostics.Tests
{
    public class TestServicesEnabled : ITest
    {

        string[] SERVICES = new string[] { "openshift.sshd", "openshift.mcollectived" };

        private ExitCode exitCode = ExitCode.PASS;

        public string GetName()
        {
            return "test_services_enabled";
        }

        public void Run()
        {
            Output.WriteDebug("Checking that required services are running now");
            List<string> missingServices = new List<string>();
            List<string> stoppedServices = new List<string>();
            List<string> noBoot = new List<string>();

            foreach (string serviceName in SERVICES)
            {

                ServiceControllerExt sc = new ServiceControllerExt(serviceName);
                
                try
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        stoppedServices.Add(serviceName);
                    }
                    if (sc.GetStartupType().ToLower() != "auto")
                    {
                        noBoot.Add(serviceName);
                    }
                }
                catch (InvalidOperationException)
                {
                    missingServices.Add(serviceName);
                }
            }

            if (missingServices.Count != 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("The following service(s) are missing:");
                sb.AppendLine(String.Join(", ", missingServices.ToArray()));
                sb.AppendLine("These services are required for OpenShift functionality.");
                Output.WriteFail(sb.ToString());
                exitCode = ExitCode.FAIL;
            }

            if (stoppedServices.Count != 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("The following service(s) are stopped:");
                sb.AppendLine(String.Join(", ", stoppedServices.ToArray()));
                sb.AppendLine("These services are required for OpenShift functionality.");
                Output.WriteFail(sb.ToString());
                exitCode = ExitCode.FAIL;
            }

            if (noBoot.Count != 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("The following service(s) are not started at boot time:");
                sb.AppendLine(String.Join(", ", noBoot.ToArray()));
                sb.AppendLine("These services are required for OpenShift functionality.");
                sb.AppendLine("Please ensure that they start at boot.");
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
