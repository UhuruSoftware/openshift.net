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
            try
            {
                WriteVerbose(string.Format("INFO: loading node configuration file {0}", NodeConfig.NodeConfigFile));
                string gearBaseDir = NodeConfig.Values["GEAR_BASE_DIR"];

                WriteVerbose(string.Format("INFO: checking node public hostname resolution"));

                try
                {
                    bool resolvesOk = Dns.Resolve(NodeConfig.Values["PUBLIC_HOSTNAME"]).AddressList.Select(ip => ip.ToString()).Contains(NodeConfig.Values["PUBLIC_IP"]);

                    if (resolvesOk)
                    {
                        WriteVerbose(string.Format("INFO: {0} resolves to {1}", NodeConfig.Values["PUBLIC_HOSTNAME"], NodeConfig.Values["PUBLIC_IP"]));
                    }
                    else
                    {
                        WriteVerbose(string.Format("ERROR: {0} does not resolve to {1}", NodeConfig.Values["PUBLIC_HOSTNAME"], NodeConfig.Values["PUBLIC_IP"]));
                    }
                }
                catch (SocketException)
                {
                    WriteVerbose(string.Format("ERROR: DNS cannot resolve {0}", NodeConfig.Values["PUBLIC_HOSTNAME"]));
                }
            }
            catch (Exception ex)
            {
                this.WriteObject(ex.ToString());
            }
        }
    }
}
