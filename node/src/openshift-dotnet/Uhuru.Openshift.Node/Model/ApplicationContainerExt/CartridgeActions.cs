using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uhuru.Openshift.Common.Models;
using Uhuru.Openshift.Common.Utils;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Utils;

namespace Uhuru.Openshift.Runtime
{
    public partial class ApplicationContainer
    {
        public string Deconfigure(string cartName)
        {
            return this.Cartridge.Deconfigure(cartName);
        }

        public string CreatePublicEndpoints(string cartName)
        {
            // currently on Windows private service ports are the same as public ports

            Manifest cart = Cartridge.GetCartridge(cartName);
            StringBuilder output = new StringBuilder();
            Dictionary<string, string> env = Environ.ForGear(this.ContainerDir);

            foreach (Endpoint endpoint in cart.Endpoints)
            {
                string port = env[endpoint.PrivatePortName];
                
                this.AddEnvVar(endpoint.PublicPortName, port);

                // TODO: will have to change this once prison is integrated
                Network.OpenFirewallPort(port, this.Uuid);

                output.Append(this.GenerateEndpointCreationNotificationMsg(cart, endpoint, "127.0.0.1", port));
            }

            return output.ToString();
        }

        public string DeletePublicEndpoint()
        {
            return string.Empty;
        }

        public string GenerateEndpointCreationNotificationMsg(Manifest cart, Endpoint endpoint, string privateIpValue, string publicPortValue)
        {

            Dictionary<string, object> endpointCreateHash = new Dictionary<string, object>()
            {
                { "cartridge_name", string.Format("{0}-{1}", cart.Name, cart.Version) },
                { "external_address", NodeConfig.Values["PUBLIC_IP"] },
                { "external_port", publicPortValue },
                { "internal_address", privateIpValue },
                { "internal_port", endpoint.PrivatePort },
                { "protocols", endpoint.Protocols },
                { "description", endpoint.Description },
                { "type", new string[0] }
            };

            if (cart.Categories.Contains("web_framework"))
            {
                endpointCreateHash["type"] = new string[] { "web_framework" };
            }
            else if (cart.Categories.Contains("database"))
            {
                endpointCreateHash["type"] = new string[] { "database" };
            }
            else if (cart.Categories.Contains("plugin"))
            {
                endpointCreateHash["type"] = new string[] { "plugin" };
            }
            else
            {
                endpointCreateHash["type"] = new string[] { "other" };
            }

            if (endpoint.Mappings != null)
            {
                endpointCreateHash["mappings"] = endpoint.Mappings.Select(m =>
                {
                    return new Dictionary<string, string>()
                    {
                        { "frontend", m.Frontend },
                        { "backend", m.Backend }
                    };
                }).ToArray();
            }

            return string.Format("NOTIFY_ENDPOINT_CREATE: {0}\n", JsonConvert.SerializeObject(endpointCreateHash));
        }

    }
}
