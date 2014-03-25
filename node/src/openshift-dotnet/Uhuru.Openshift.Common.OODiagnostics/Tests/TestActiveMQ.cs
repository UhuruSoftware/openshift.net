using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Common.OODiagnostics.Tests
{
    public class TestActiveMQ :ITest
    {
        ExitCode exitCode = ExitCode.PASS;

        public string GetName()
        {
            return "test_active_mq";
        }

        public void Run()
        {
            Output.WriteDebug("Checking Active MQ");
            Config srvConfig = Helpers.GetMcollectiveSrvConfig();
            string activemqHost = srvConfig.Get("plugin.activemq.pool.1.host");
            string activemqPort = srvConfig.Get("plugin.activemq.pool.1.port");

            TcpClient client = new TcpClient();
            try
            {

                client.Connect(activemqHost, int.Parse(activemqPort));
                
            }
            catch (SocketException)
            {
                Output.WriteFail(string.Format("Could not establish TCP connection to Active MQ server at {0}:{1}", activemqHost, activemqPort));
                exitCode = ExitCode.FAIL;
            }
            
        }

        public ExitCode GetExitCode()
        {
            return exitCode;
        }
    }
}
