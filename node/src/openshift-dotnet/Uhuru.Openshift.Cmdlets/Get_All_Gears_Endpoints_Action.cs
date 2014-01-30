using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Utils;
using Uhuru.Openshift.Utilities;


namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("Get", "All-Gears-Endpoints-Action")]
    public class Get_All_Gears_Endpoints_Action : System.Management.Automation.Cmdlet 
    {
        protected override void ProcessRecord()
        {
            ReturnStatus returnStatus = new ReturnStatus();
            try
            {
                NodeConfig config = new NodeConfig();
                string gearPath = config.Get("GEAR_BASE_DIR");
                string[] folders = Directory.GetDirectories(gearPath);
                List<RubyHash> endpoints = new List<RubyHash>();
                RubyHash outputHash = new RubyHash();
                
                
                foreach (string folder in folders)
                {
                    string  folderName = Path.GetFileName(folder);
                    if (!folderName.StartsWith("."))
                    {
                        ApplicationContainer container = ApplicationContainer.GetFromUuid(folderName);
                        Dictionary<string,string> env = Environ.ForGear(container.ContainerDir);

                        container.Cartridge.EachCartridge(cart =>
                        {
                            cart.Endpoints.ForEach(endpoint =>
                                {
                                    RubyHash endpointHash = new RubyHash();
                                    
                                    endpointHash.Add("cartridge_name", string.Format("{0}-{1}", cart.Name, cart.Version));
                                    if (env.ContainsKey(endpoint.PublicPortName))
                                    {
                                        endpointHash.Add("external_port", env[endpoint.PublicPortName]);
                                    }
                                    else
                                    {
                                        endpointHash.Add("external_port", null);
                                    }
                                    endpointHash.Add("internal_address", env[endpoint.PrivateIpName]);
                                    endpointHash.Add("internal_port", endpoint.PrivatePort);
                                    endpointHash.Add("protocols", endpoint.Protocols);
                                    endpointHash.Add("type", new List<string>());

                                    if (cart.WebProxy)
                                    {
                                        endpointHash["protocols"] = container.Cartridge.GetPrimaryCartridge().Endpoints.First().Protocols;
                                        endpointHash["type"] = new List<string>() {"load_balancer"};
                                    }
                                    else if (cart.WebFramework)
                                    {
                                        endpointHash["type"] = new List<string>(){"web_framework"};
                                    }
                                    else if (cart.Categories.Contains("database"))
                                    {
                                        endpointHash["type"] = new List<string>(){"web_framework"};
                                    }
                                    else if (cart.Categories.Contains("plugin"))
                                    {
                                        endpointHash["type"] = new List<string>(){"plugin"};
                                    }
                                    else
                                    {
                                        endpointHash["type"] = new List<string>(){"other"};
                                    }

                                    if (endpoint.Mappings != null && endpoint.Mappings.Count > 0)
                                    {
                                        List<RubyHash> mappingsList = new List<RubyHash>();
                                        foreach (Uhuru.Openshift.Common.Models.Endpoint.Mapping mapping in endpoint.Mappings)
                                        {
                                            RubyHash mappings = new RubyHash();
                                            mappings.Add("frontend", mapping.Frontend);
                                            mappings.Add("backend", mapping.Backend);
                                            mappingsList.Add(mappings);
                                        }
                                        endpointHash.Add("mappings", mappingsList);
                                    }
                                    endpoints.Add(endpointHash);
                                });
                        });
                        if (endpoints.Count > 0)
                        {
                            outputHash.Add(folderName, endpoints);
                        }
                    }
                }
                string output = Newtonsoft.Json.JsonConvert.SerializeObject(outputHash);
                returnStatus.Output = output;
                returnStatus.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running get-all-gears-endpoints-action command: {0} - {1}", ex.Message, ex.StackTrace);
                returnStatus.Output = ex.ToString();
                returnStatus.ExitCode = 1;

            }
            this.WriteObject(returnStatus);
        }
    }
}
