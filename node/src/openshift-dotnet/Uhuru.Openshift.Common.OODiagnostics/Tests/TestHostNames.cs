using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Common.OODiagnostics.Tests
{
    public class TestHostNames :ITest
    {

        ExitCode exitCode = ExitCode.PASS;

        public string GetName()
        {
            return "test_host_names";
        }

        public void Run()
        {
            Output.WriteDebug("Checking that the broker hostname resolves");

            string brokerHostname = Helpers.GetNodeConfig().Get("BROKER_HOST");

            try
            {
                IPHostEntry iphostEntry = Dns.GetHostEntry(brokerHostname);
            }
            catch (SocketException)
            {
                Output.WriteFail( string.Format("Broker hostname {0} is not resolved",brokerHostname));
                exitCode = ExitCode.FAIL;
            }
               
        }

        public ExitCode GetExitCode()
        {
            return exitCode;
        }
    }
}
