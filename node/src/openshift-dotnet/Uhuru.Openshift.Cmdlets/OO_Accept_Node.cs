using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Config;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Accept-Node")]
    public class OO_Accept_Node : System.Management.Automation.Cmdlet
    {
        [Parameter]
        public decimal Timeout;

        [Parameter]
        public SwitchParameter RunUpgradeChecks;

        protected override void ProcessRecord()
        {
            ReturnStatus status = new ReturnStatus();
            StringBuilder output = new StringBuilder();
            try
            {
                output.AppendLine(string.Format("INFO: loading node configuration file {0}", NodeConfig.NodeConfigFile));
                string gearBaseDir = NodeConfig.Values["GEAR_BASE_DIR"];

                output.AppendLine(string.Format("INFO: checking node public hostname resolution"));

                try
                {
                    bool resolvesOk = Dns.Resolve(NodeConfig.Values["PUBLIC_HOSTNAME"]).AddressList.Select(ip => ip.ToString()).Contains(NodeConfig.Values["PUBLIC_IP"]);

                    if (resolvesOk)
                    {
                        output.AppendLine(string.Format("INFO: {0} resolves to {1}", NodeConfig.Values["PUBLIC_HOSTNAME"], NodeConfig.Values["PUBLIC_IP"]));
                        status.ExitCode = 0;
                    }
                    else
                    {
                        output.AppendLine(string.Format("ERROR: {0} does not resolve to {1}", NodeConfig.Values["PUBLIC_HOSTNAME"], NodeConfig.Values["PUBLIC_IP"]));
                        status.ExitCode = 1;
                    }
                }
                catch (SocketException)
                {
                    output.AppendLine(string.Format("ERROR: DNS cannot resolve {0}", NodeConfig.Values["PUBLIC_HOSTNAME"]));
                    status.ExitCode = 1;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-accept-node command: {0} - {1}", ex.Message, ex.StackTrace);
                status.ExitCode = 1;
                status.Output = ex.ToString();
            }
            this.WriteObject(status);
        }
    }
}
