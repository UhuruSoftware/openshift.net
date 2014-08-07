using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uhuru.Openshift.Common.JsonHelper;
using Uhuru.Openshift.Common.Models;
using Uhuru.Openshift.Common.Utils;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Model;
using Uhuru.Openshift.Runtime.Utils;
using Uhuru.Openshift.Utilities;

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

        public string DeletePublicEndpoints(string cartName)
        {
            Manifest cart = Cartridge.GetCartridge(cartName);
            StringBuilder output = new StringBuilder();
            Dictionary<string, string> env = Environ.ForGear(this.ContainerDir);

            try
            {
                foreach (Endpoint endpoint in cart.Endpoints)
                {
                    string port = env[endpoint.PrivatePortName];

                    // TODO: will have to change this once prison is integrated
                    Network.CloseFirewallPort(port);

                    output.AppendFormat("NOTIFY_ENDPOINT_DELETE: {0} {1}", NodeConfig.Values["PUBLIC_IP"], port);
                }

                Logger.Warning(@"Deleted all public endpoints for cart {0} in gear {1}", cartName, this.Uuid);
            }
            catch (Exception ex)
            {
                Logger.Warning(@"Couldn't delete all public endpoints for cart {0} in gear {1}: {2} - {3}", cartName, this.Uuid, ex.Message, ex.StackTrace);
            }

            return output.ToString();
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
            //this is temporary fix to test gear move possibility
            output += @"
ATTR: quota_blocks=1048576
ATTR: quota_files=40000
";
            output += this.Cartridge.DoControl("status", cartName);
            return output;
        }

        public string GetQuota(string cartName)
        {
            //TODO: we need to implement the method to get the quota from the cartridge
            string output = "1048576"; //have to clarify how the broker agent gets this information currently it's getting the 3 and 6 element ->this is just mock data

            //output += this.Cartridge.DoControl("get-quota", cartName);
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
            Logger.Debug("Running PostConfigure for '{0}' with cart '{1}' and git url '{2}'", this.Uuid, cartName, templateGitUrl);

            StringBuilder output = new StringBuilder();
            Manifest cartridge = this.Cartridge.GetCartridge(cartName);

            bool performInitialBuild = !Git.EmptyCloneSpec(templateGitUrl) && (cartridge.InstallBuildRequired || !string.IsNullOrEmpty(templateGitUrl)) && cartridge.Buildable;

            if (performInitialBuild)
            {
                Logger.Info("Performing initial build");
                try
                {
                    RunProcessInContainerContext(this.ContainerDir, "gear prereceive --init");
                    RunProcessInContainerContext(this.ContainerDir, "gear postreceive --init");                   
                }
                catch (Exception ex)
                {
                    // TODO: vladi: implement exception handling for initial build
                }
            }
            else if (cartridge.Deployable)
            {
                string deploymentDatetime = LatestDeploymentDateTime();
                DeploymentMetadata deploymentMetadata = DeploymentMetadataFor(deploymentDatetime);
                if (deploymentMetadata.Activations.Count == 0)
                {
                    Prepare(new RubyHash() { { "deployment_datetime", deploymentDatetime } });
                    deploymentMetadata.Load();
                    ApplicationRepository applicationRepository = new ApplicationRepository(this);
                    string gitRef = "master";
                    string gitSha1 = applicationRepository.GetSha1(gitRef);
                    string deploymentsDir = Path.Combine(this.ContainerDir, "app-deployments");
                    SetRWPermissions(deploymentsDir);
                    // TODO: reset_permission_R(deployments_dir)

                    deploymentMetadata.RecordActivation();
                    deploymentMetadata.Save();

                    UpdateCurrentDeploymentDateTimeSymlink(deploymentDatetime);
                    
                    FixHomeDir();
                }
            }

            output.AppendLine(this.Cartridge.PostConfigure(cartName));

            if (performInitialBuild)
            {
                // TODO: grep build log
            }

            return output.ToString();
        }

        public void PostReceive(RubyHash options)
        {
            Logger.Debug("Running post receive for gear {0}", this.Uuid);

            Dictionary<string, string> gearEnv = Environ.ForGear(this.ContainerDir);
            
            string repoDir = Path.Combine(this.ContainerDir, "app-root", "runtime", "repo");

            Directory.CreateDirectory(repoDir);

            ApplicationRepository applicationRepository = new ApplicationRepository(this);
            applicationRepository.Archive(repoDir, options["ref"]);

            options["deployment_datetime"] = this.LatestDeploymentDateTime();

            Build(options);

            Logger.Debug("Running post receive - prepare for gear {0}", this.Uuid);
            Prepare(options);

            Logger.Debug("Running post receive - distribute for gear {0}", this.Uuid);
            Distribute(options);

            Logger.Debug("Running post receive - activate for gear {0}", this.Uuid);
            Activate(options);
        }

        public string Build(RubyHash options)
        {
            this.State.Value(Runtime.State.BUILDING);
            string deploymentDateTime = options["deployment_datetime"] != null ? options["deployment_datetime"] : LatestDeploymentDateTime();
            DeploymentMetadata deploymentMetadata = DeploymentMetadataFor(deploymentDateTime);

            if (!options.ContainsKey("deployment_datetime"))
            {
                // this will execute if coming from a CI builder, since it doesn't
                // specify :deployment_datetime in the options hash
                ApplicationRepository applicationRepository = options["git_repo"];
                string gitRef = options["ref"];
                string gitSha1 = applicationRepository.GetSha1(gitRef);
                deploymentMetadata.GitSha = gitSha1;
                deploymentMetadata.GitRef = gitRef;
                deploymentMetadata.HotDeploy = options["hot_deploy"];
                deploymentMetadata.ForceCleanBuild = options["force_clean_build"];
                deploymentMetadata.Save();
            }

            StringBuilder buffer = new StringBuilder();

            if(deploymentMetadata.ForceCleanBuild)
            {
                buffer.AppendLine("Force clean build enabled - cleaning dependencies");

                CleanRuntimeDirs(new RubyHash() { { "dependencies", true }, { "build_dependencies", true } });

                this.Cartridge.EachCartridge(delegate(Manifest cartridge) {
                    this.Cartridge.CreateDependencyDirectories(cartridge);
                });
            }

            buffer.AppendLine(string.Format("Building git ref {0}, commit {1}", deploymentMetadata.GitRef, deploymentMetadata.GitSha));

            Dictionary<string, string> env = Environ.ForGear(this.ContainerDir);
            int deploymentsToKeep = DeploymentsToKeep(env);

            try
            {
                Manifest primaryCartridge = this.Cartridge.GetPrimaryCartridge();
                buffer.AppendLine(this.Cartridge.DoControl("update-configuration", primaryCartridge, new RubyHash() { { "pre_action_hooks_enabled", false }, { "post_action_hooks_enabled", false } }));
                buffer.AppendLine(this.Cartridge.DoControl("pre-build", primaryCartridge, new RubyHash() { { "pre_action_hooks_enabled", false }, { "post_action_hooks_enabled", false } }));
                buffer.AppendLine(this.Cartridge.DoControl("build", primaryCartridge, new RubyHash() { { "pre_action_hooks_enabled", false }, { "post_action_hooks_enabled", false } }));
            }
            catch(Exception ex)
            {
                buffer.AppendLine("Encountered a failure during build: " + ex.ToString());
                if(deploymentsToKeep > 1)
                {
                    buffer.AppendLine("Restarting application");
                    buffer.AppendLine(StartGear(new RubyHash() { { "user_initiated", true }, { "hot_deploy", deploymentMetadata.HotDeploy } }));
                }
                throw ex;
            }

            return buffer.ToString();
        }

        public string Activate(RubyHash options = null)
        {
            Logger.Debug("Activating gear {0}", this.Uuid);

            if(options == null)
            {
                options = new RubyHash();
            }

            bool useOutput = options.ContainsKey("out") && options["out"];

            StringBuilder output = new StringBuilder();
            dynamic result = new Dictionary<string, object>();

            if (useOutput)
            {
                output.Append("Activating deployment");
            }

            if (!options.ContainsKey("deployment_id"))
            {
                throw new Exception("deployment_id must be supplied");
            }
            string deploymentId = options["deployment_id"];
            string deploymentDateTime = GetDeploymentDateTimeForDeploymentId(deploymentId);
            DeploymentMetadata deploymentMetadata = DeploymentMetadataFor(deploymentDateTime);
       
            options["hot_deploy"] = deploymentMetadata.HotDeploy;
            if (options.ContainsKey("post_install") || options.ContainsKey("restore"))
            {
                options["hot_deploy"] = false;
            }

            List<RubyHash> parallelResults = WithGearRotation(options, (GearRotationCallback)delegate(object targetGear, Dictionary<string, string> localGearEnv, RubyHash opts)
                {
                    string targetGearUuid;
                    if (targetGear is string)
                    {
                        targetGearUuid = targetGear.ToString();
                    }
                    else
                    {
                        targetGearUuid = ((Model.GearRegistry.Entry)targetGear).Uuid;
                    }
                    if (targetGearUuid == this.Uuid)
                    {
                        return ActivateLocalGear(options);
                    }
                    else
                    {                        
                        return ActivateRemoteGear((GearRegistry.Entry)targetGear, localGearEnv, options);
                    }
                });

            List<string> activatedGearUuids = new List<string>();

            if ((options.ContainsKey("all") && options["all"]) || (options.ContainsKey("gears") && options["gears"]))
            {
                result["status"] = RESULT_SUCCESS;
                result["gear_results"] = new Dictionary<string, object>();

                foreach (RubyHash gearResult in parallelResults)
                {
                    string gearUuid = gearResult["gear_uuid"];
                    activatedGearUuids.Add(gearUuid);

                    result["gear_results"][gearUuid] = gearResult;

                    if (gearResult["status"] != RESULT_SUCCESS)
                    {
                        result["status"] = RESULT_FAILURE;
                    }
                }
            }
            else
            {
                activatedGearUuids.Add(this.Uuid);
                result = parallelResults[0];
            }

            output.Append(JsonConvert.SerializeObject(result));

            return output.ToString();
        }

        /// <summary>
        /// Unsubscribes from a cartridge
        /// </summary>
        /// <param name="cartName">Unsubscribing cartridge name.</param>
        /// <param name="pubCartName">Publishing cartridge name.</param>
        /// <returns>The output</returns>
        public string Unsubscribe(string cartName, string pubCartName)
        {
            Cartridge.Unsubscribe(cartName, pubCartName);
            return string.Empty;
        }

        private RubyHash ActivateLocalGear(dynamic options)
        {
            string deploymentId = options["deployment_id"];

            Logger.Debug("Activating local gear with deployment id {0}", deploymentId);

            RubyHash result = new RubyHash();
            result["status"] = RESULT_FAILURE;
            result["gear_uuid"] = this.Uuid;
            result["deployment_id"] = deploymentId;
            result["messages"] = new List<string>();
            result["errors"] = new List<string>();

            if (!DeploymentExists(deploymentId))
            {
                Logger.Warning("No deployment with id {0} found on gear", deploymentId);
                result["errors"].Add(string.Format("No deployment with id {0} found on gear", deploymentId));
                return result;
            }

            try
            {
                string deploymentDateTime = GetDeploymentDateTimeForDeploymentId(deploymentId);
                string deploymentDir = Path.Combine(this.ContainerDir, "app-deployments", deploymentDateTime);

                Dictionary<string, string> gearEnv = Environ.ForGear(this.ContainerDir);

                string output = string.Empty;

                Logger.Debug("Current deployment state for deployment {0} is {1}", deploymentId, this.State.Value());

                if (Runtime.State.STARTED.EqualsString(State.Value()))
                {
                    options["exclude_web_proxy"] = true;
                    output = StopGear(options);
                    result["messages"].Add(output);
                }

                SyncDeploymentRepoDirToRuntime(deploymentDateTime);
                SyncDeploymentDependenciesDirToRuntime(deploymentDateTime);
                SyncDeploymentBuildDependenciesDirToRuntime(deploymentDateTime);

                UpdateCurrentDeploymentDateTimeSymlink(deploymentDateTime);

                FixHomeDir();

                Manifest primaryCartridge = this.Cartridge.GetPrimaryCartridge();
                
                this.Cartridge.DoControl("update-configuration", primaryCartridge);

                result["messages"].Add("Starting application " + ApplicationName);

                Dictionary<string, object> opts = new Dictionary<string,object>();
                opts["secondary_only"] = true;
                opts["user_initiated"] = true;
                opts["hot_deploy"] = options["hot_deploy"];

                output = StartGear(opts);
                result["messages"].Add(output);

                this.State.Value(Runtime.State.DEPLOYING);

                opts = new Dictionary<string, object>();
                opts["pre_action_hooks_enabled"] = false;
                opts["prefix_action_hooks"] = false;
                output = this.Cartridge.DoControl("deploy", primaryCartridge, opts);
                result["messages"].Add(output);

                opts = new Dictionary<string, object>();
                opts["primary_only"] = true;
                opts["user_initiated"] = true;
                opts["hot_deploy"] = options["hot_deploy"];
                output = StartGear(opts);
                result["messages"].Add(output);

                opts = new Dictionary<string, object>();
                opts["pre_action_hooks_enabled"] = false;
                opts["prefix_action_hooks"] = false;
                output = this.Cartridge.DoControl("post-deploy", primaryCartridge, opts);
                result["messages"].Add(output);

                if (options.ContainsKey("post_install"))
                {
                    string primaryCartEnvDir = Path.Combine(this.ContainerDir, primaryCartridge.Dir, "env");
                    Dictionary<string, string> primaryCartEnv = Environ.Load(primaryCartEnvDir);
                    string ident = (from kvp in primaryCartEnv
                                    where Regex.Match(kvp.Key, "^OPENSHIFT_.*_IDENT").Success
                                    select kvp.Value).FirstOrDefault();
                    string version = Manifest.ParseIdent(ident)[2];
                    this.Cartridge.PostInstall(primaryCartridge, version);
                }

                DeploymentMetadata deploymentMetadata = DeploymentMetadataFor(deploymentDateTime);
                deploymentMetadata.RecordActivation();
                deploymentMetadata.Save();

                if (options.ContainsKey("report_deployments") && gearEnv["OPENSHIFT_APP_DNS"] == gearEnv["OPENSHIFT_GEAR_DNS"])
                {
                    ReportDeployments(gearEnv);
                }

                result["status"] = RESULT_SUCCESS;
            }
            catch(Exception e)
            {
                result["status"] = RESULT_FAILURE;
                result["errors"].Add(string.Format("Error activating gear: {0}", e.ToString()));
            }

            return result;
        }

        private RubyHash ActivateRemoteGear(GearRegistry.Entry gear, Dictionary<string, string> gearEnv, RubyHash options)
        {
            string gearUuid = gear.Uuid;

            RubyHash result = new RubyHash();
            result["status"] = RESULT_FAILURE;
            result["gear_uuid"] = this.Uuid;
            result["deployment_id"] = options["deployment_id"];
            result["messages"] = new List<string>();
            result["errors"] = new List<string>();

            string postInstallOptions = options["post_install"] == true ? "--post-install" : "";

            result["messages"].Add(string.Format("Activating gear {0}, deployment id: {1}, {2}", gearUuid, options["deployment_id"], postInstallOptions));
            try
            {
                string ooSSH = @"/cygpath/c/openshift/oo-bin/oo-ssh";
                string bashBinary = Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], "bin\bash.exe");

                string sshCommand = string.Format("{0} {1} gear activate {2} --as-json {3} --no-rotation",
                    ooSSH, gear.ToSshUrl(), options["deployment_id"], postInstallOptions);

                string bashArgs = string.Format("--norc --login -c '{0}'", sshCommand);

                string command = string.Format("{0} {1}", bashBinary, bashArgs);

                string output = RunProcessInContainerContext(this.ContainerDir, command).StdOut;

                if (string.IsNullOrEmpty(output))
                {
                    throw new Exception("No result JSON was received from the remote activate call");
                }
                Dictionary<string, object> activateResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
                if (!activateResult.ContainsKey("status"))
                {
                    throw new Exception("Invalid result JSON received from remote activate call");
                }

                result["messages"].Add(activateResult["messages"]);
                result["errors"].Add(activateResult["errors"]);
                result["status"] = activateResult["status"];
            }
            catch (Exception e)
            {
                result["errors"].Add("Gear activation failed: " + e.ToString());                
            }

            return result;
        }

        public string Deploy(RubyHash options)
        {
            StringBuilder output = new StringBuilder();
            if (!options.ContainsKey("artifact_url"))
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

        public string Prepare(RubyHash options = null)
        {
            if (options == null)
            {
                options = new RubyHash();
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
                Logger.Error("Error preparing deployment. Options: {0} - {1} - {2}", JsonConvert.SerializeObject(options), e.Message, e.StackTrace);
                output.AppendLine("Error preparing deployment " + deploymentId);
                UnlinkDeploymentId(deploymentId);
            }

            return output.ToString();
        }

        private string DeployBinaryArtifact(dynamic options)
        {
            throw new NotImplementedException();
        }

        private void FixHomeDir()
        {
            string userHomeDir = this.ContainerDir;
            string username = null;
            string gearUuid = this.Uuid;
            if (Environment.UserName.StartsWith(Prison.PrisonUser.GlobalPrefix))
            {
                username = Environment.UserName;
            }
            else
            {
                username = Prison.Prison.LoadPrisonAndAttach(PrisonIdConverter.Generate(this.Uuid)).User.Username;
            }
            if (!string.IsNullOrEmpty(userHomeDir))
            {
                LinuxFiles.TakeOwnershipOfGearHome(userHomeDir, username);

                Logger.Debug("Fixing symlinks for gear {0}", gearUuid);
                try
                {
                    LinuxFiles.FixSymlinks(Path.Combine(userHomeDir, "app-deployments"));
                }
                catch (Exception ex)
                {
                    Logger.Error("There was an error while trying to fix symlinks for gear {0}: {1} - {2}", gearUuid, ex.Message, ex.StackTrace);
                }
            }
            else
            {
                Logger.Warning("Not taking ownership or fixing symlinks for gear {0}. Could not locate its home directory.", gearUuid);
            }
        }
    }
}
