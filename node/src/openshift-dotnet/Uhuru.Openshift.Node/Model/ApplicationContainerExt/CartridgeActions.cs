using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

        public string GetStatus(string cartName)
        {
            string output = StoppedStatusAttr();

            //TODO: we need to implement the windows prison to get the disk quota for the user

            output += this.Cartridge.DoControl("status", cartName);
            return output;
        }

        public string PreReceive(dynamic options)
        {
            options["excludeWebProxy"] = true;
            options["userInitiated"] = true;
            StopGear(options);
            CreateDeploymentDir();

            return string.Empty;
        }

        public string PostConfigure(string cartName, string templateGitUrl = null)
        {
            StringBuilder output = new StringBuilder();
            Manifest cartridge = this.Cartridge.GetCartridge(cartName);

            bool performInitialBuild = !Git.EmptyCloneSpec(templateGitUrl) && (cartridge.InstallBuildRequired || !string.IsNullOrEmpty(templateGitUrl)) && cartridge.Buildable;

            if (performInitialBuild)
            {
                Dictionary<string, string> env = Environ.ForGear(this.ContainerDir);
                output.AppendLine(RunProcessInContainerContext(this.ContainerDir, "gear -Prereceive -Init"));
                output.AppendLine(RunProcessInContainerContext(this.ContainerDir, "gear -Postreceive -Init"));
            }
            else if (cartridge.Deployable)
            {
                string deploymentDatetime = LatestDeploymentDateTime();
                DeploymentMetadata deploymentMetadata = DeploymentMetadataFor(deploymentDatetime);
                if (deploymentMetadata.Activations.Count == 0)
                {
                    Prepare(new Dictionary<string, object>() { { "deployment_datetime", deploymentDatetime } });
                    deploymentMetadata.Load();
                    ApplicationRepository applicationRepository = new ApplicationRepository(this);
                    string gitRef = "master";
                    string gitSha1 = applicationRepository.GetSha1(gitRef);
                    string deploymentsDir = Path.Combine(this.ContainerDir, "app-deployments");
                    SetRWPermissions(deploymentsDir);
                    // TODO reset_permission_R(deployments_dir)

                    deploymentMetadata.RecordActivation();
                    deploymentMetadata.Save();

                    UpdateCurrentDeploymentDateTimeSymlink(deploymentDatetime);
                }
            }

            output.AppendLine(this.Cartridge.PostConfigure(cartName));

            if (performInitialBuild)
            {
                // grep build log
            }

            return output.ToString();
        }

        public void PostReceive(dynamic options)
        {

            Dictionary<string, string> gearEnv = Environ.ForGear(this.ContainerDir);
            
            string repoDir = Path.Combine(this.ContainerDir, "app-root", "runtime", "repo");

            Directory.CreateDirectory(repoDir);

            ApplicationRepository applicationRepository = new ApplicationRepository(this);
            applicationRepository.Archive(repoDir, options["ref"]);

            Distribute(options);
            Activate(options);
        }

        public string Activate(dynamic options)
        {
            StringBuilder output = new StringBuilder();

            if (!options.ContainsKey("deployment_id"))
            {
                throw new Exception("deployment_id must be supplied");
            }
            string deploymentId = options["deployment_id"];


            Dictionary<string, object> opts = new Dictionary<string, object>();
            opts["secondaryOnly"] = true;
            opts["userInitiated"] = true;
            //opts["hotDeploy"] = options["hotDeploy"];
            StartGear(opts);

            return string.Empty;
        }

        public string Deploy(dynamic options)
        {
            StringBuilder output = new StringBuilder();
            if (!((Dictionary<string, object>)options).ContainsKey("artifact_url"))
            {
                output.AppendLine(PreReceive(options));
                PostReceive(options);
            }
            else
            {
                output.AppendLine(DeployBinaryArtifact(options));
            }
            return output.ToString();
        }

        public string Prepare(Dictionary<string, object> options = null)
        {
            if (options == null)
            {
                options = new Dictionary<string, object>();
            }
            StringBuilder output = new StringBuilder();
            output.AppendLine("Preparing build for deployment");
            if (!options.ContainsKey("deployment_datetime"))
            {
                throw new ArgumentException("deployment_datetime is required");
            }
            string deploymentDatetime = options["deployment_datetime"].ToString();
            Dictionary<string, string> env = Environ.ForGear(this.ContainerDir);

            // TODO clean runtime dirs, extract archive

            this.Cartridge.DoActionHook("prepare", env, options);
            string deploymentId = CalculateDeploymentId();
            LinkDeploymentId(deploymentDatetime, deploymentId);

            try
            {
                SyncRuntimeRepoDirToDeployment(deploymentDatetime);
                SyncRuntimeDependenciesDirToDeployment(deploymentDatetime);
                SyncRuntimeBuildDependenciesDirToDeployment(deploymentDatetime);

                DeploymentMetadata deploymentMetadata = DeploymentMetadataFor(deploymentDatetime);
                deploymentMetadata.Id = deploymentId;
                deploymentMetadata.Checksum = CalculateDeploymentChecksum(deploymentId);
                deploymentMetadata.Save();

                options["deployment_id"] = deploymentId;
                output.AppendLine("Deployment id is " + deploymentId);
            }
            catch (Exception e)
            {
                output.AppendLine("Error preparing deployment " + deploymentId);
                UnlinkDeploymentId(deploymentId);
            }

            return output.ToString();
        }

        private string DeployBinaryArtifact(dynamic options)
        {
            throw new NotImplementedException();
        }
    }
}
