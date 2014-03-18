using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Utils;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Cmdlets
{
    [Alias("Gear")]
    [Cmdlet("OO", "Gear")]
    public class Gear : Cmdlet
    {
        [Parameter(HelpMessage = "Proceed even if the git ref to deploy isn't found.", ParameterSetName = "Prereceive")]
        [Parameter(ParameterSetName = "Postreceive")]
        public SwitchParameter Init;

        [Parameter(HelpMessage = "Run the git prereceive steps", ParameterSetName = "Prereceive")]
        public SwitchParameter Prereceive;

        [Parameter(HelpMessage = "Run the git postreceive steps", ParameterSetName = "Postreceive")]
        public SwitchParameter Postreceive;

        [Parameter(HelpMessage = "Run the build steps", ParameterSetName = "Build")]
        public SwitchParameter Build;

        [Parameter(HelpMessage = "Prepare a binary deployment artifact for distribution and activation", ParameterSetName = "Prepere", Position = 0)]
        public SwitchParameter Prepare;

        [Parameter(HelpMessage = "Prepare a binary deployment artifact for distribution and activation", ParameterSetName = "Prepere", Position = 1)]
        public string File;

        [Parameter(HelpMessage = "Distribute a build", ParameterSetName = "Distribute" )]
        public SwitchParameter Distribute;

        [Parameter(HelpMessage = "Deployment ID", ParameterSetName = "Distribute")]
        [Parameter(ParameterSetName = "ArchiveDeployment")]
        [Parameter(ParameterSetName = "Activate", Position=1)]
        public string DeploymentId;

        [Parameter(HelpMessage = "Activate a build", ParameterSetName = "Activate", Position=0)]
        public SwitchParameter Activate;

        [Parameter(HelpMessage= "Run post_install for new gears", ParameterSetName= "Activate")]
        public SwitchParameter PostInstall;

        [Parameter(HelpMessage = "Render the results as JSON to stdout", ParameterSetName = "Activate")]
        [Parameter(ParameterSetName="Restart")]
        [Parameter(ParameterSetName = "RotateOut")]
        [Parameter(ParameterSetName = "RotateIn")]
        public SwitchParameter AsJson;

        [Parameter(HelpMessage = "Rotate gears out/in (defaults to true)", ParameterSetName = "Activate")]
        public SwitchParameter Rotation;

        [Parameter(HelpMessage = "Do not rotate gears out/in", ParameterSetName = "Activate")]
        public SwitchParameter NoRotation;

        [Parameter(HelpMessage = "Archive the current deployment", ParameterSetName = "ArchiveDeployment" )]
        public SwitchParameter ArchiveDeployment;

        [Parameter(HelpMessage = "Create a deployment directory. Should only be used by CI builders", ParameterSetName = "CreateDeploymentDir")]
        public SwitchParameter CreateDeploymentDir;

        [Parameter(HelpMessage = "Run the remotedeploy steps", ParameterSetName = "RemoteDeploy" )]
        public SwitchParameter RemoteDeploy;

        [Parameter(HelpMessage = "Datetime to deploy", ParameterSetName = "RemoteDeploy")]
        public string DeploymentDateTimeName;

        [Parameter(HelpMessage = "Run the deploy steps", ParameterSetName = "Deploy" )]
        public SwitchParameter Deploy;

        [Parameter(HelpMessage = "Perform hot deployment", ParameterSetName = "Deploy")]
        public SwitchParameter HotDeploy;

        [Parameter(HelpMessage = "Perform a clean build", ParameterSetName = "Deploy")]
        public SwitchParameter ForceCleanBuild;

        [Parameter(HelpMessage = "Deploy Reference ID", ParameterSetName = "Deploy")]
        public string DeployRefId;

        [Parameter(HelpMessage = "Deploy a binary artifact", ParameterSetName = "BinaryDeploy" )]
        public SwitchParameter BinaryDeploy;

        [Parameter(HelpMessage = "List the gear's deployments", ParameterSetName = "Deployments" )]
        public SwitchParameter Deployments;

        [Parameter(HelpMessage = "The cart to use", ParameterSetName = "Start")]
        [Parameter(ParameterSetName = "Stop")]
        [Parameter(ParameterSetName = "Restart")]
        [Parameter(ParameterSetName = "Reload")]
        [Parameter(ParameterSetName = "Status")]
        [Parameter(ParameterSetName = "RotateOut")]
        [Parameter(ParameterSetName = "RotateIn")]
        public string Cart;

        [Parameter(HelpMessage = "Start the gear/cart", ParameterSetName = "Start")]
        public SwitchParameter Start;

        [Parameter(HelpMessage = "Stop the gear/cart", ParameterSetName = "Stop")]
        public SwitchParameter Stop;

        [Parameter(HelpMessage = "Skip the gear stop if the hot deploy marker is present in the application Git repo in the commit specified by --git-ref", ParameterSetName = "Stop")]
        public SwitchParameter Conditional;

        [Parameter(HelpMessage = "The git ref to use when checking for the presence of the hot deploy marker file", ParameterSetName = "Stop")]
        public string GitRef;

        [Parameter(HelpMessage = "Skip stopping the web proxy, if it exists", ParameterSetName = "Stop")]
        public SwitchParameter ExclusiveWebProxy;

        [Parameter(HelpMessage = "Restart a cart", ParameterSetName = "Restart")]
        public SwitchParameter Restart;

        [Parameter(HelpMessage = "Restart all instances of the specified cartridge for all gears for this application", ParameterSetName = "Restart")]
        public SwitchParameter All;

        [Parameter(HelpMessage = "Reload a cart", ParameterSetName = "Reload")]
        public SwitchParameter Reload;

        [Parameter(HelpMessage = "Get the status for a cart", ParameterSetName = "Status")]
        public SwitchParameter Status;

        [Parameter(HelpMessage = "Snapshot an application", ParameterSetName = "Snapshot")]
        public SwitchParameter Snapshot;

        [Parameter(HelpMessage = "Restore an application", ParameterSetName = "Restore")]
        public SwitchParameter Restore;

        [Parameter(HelpMessage = "Rebuild the application as part of restoration", ParameterSetName = "Restore")]
        public SwitchParameter RestoreGitRepo;

        [Parameter(HelpMessage = "Do not report a deployment after restoration", ParameterSetName = "Restore")]
        public SwitchParameter NoReportDeployments;

        [Parameter(HelpMessage = "Disables this gear from receiving traffic from the proxy", ParameterSetName = "RotateOut")]
        public SwitchParameter RotateOut;

        [Parameter(HelpMessage = "Store the disabling of this gear in the proxy configuration file", ParameterSetName = "RotateOut")]
        [Parameter(ParameterSetName="RotateIn")]
        public SwitchParameter Persist;

        [Parameter(HelpMessage = "UUID of the gear to disable", ParameterSetName = "RotateOut")]
        [Parameter(ParameterSetName = "RotateIn")]
        public string GearUUID;

        [Parameter(HelpMessage = "Enables this gear to receive traffic from the proxy", ParameterSetName = "RotateIn")]
        public SwitchParameter RotateIn;

        ApplicationContainer container = null;
        ApplicationRepository repo = null;

        protected override void ProcessRecord()
        {            
            this.WriteObject(Execute());
        }
        
        public ReturnStatus Execute()
        {
            Logger.Debug("Running gear command: '{0}'", Environment.CommandLine);

            ReturnStatus status = new ReturnStatus();
            try
            {
                string appUuid = Environment.GetEnvironmentVariable("OPENSHIFT_APP_UUID");
                string gearUuid = Environment.GetEnvironmentVariable("OPENSHIFT_GEAR_UUID");
                string appName = Environment.GetEnvironmentVariable("OPENSHIFT_APP_NAME");
                string gearName = Environment.GetEnvironmentVariable("OPENSHIFT_GEAR_NAME");
                string nmSpace = Environment.GetEnvironmentVariable("OPENSHIFT_NAMESPACE");

                NodeConfig config = new NodeConfig();
                EtcUser etcUser = new Etc(config).GetPwanam(gearUuid);

                container = new ApplicationContainer(appUuid, gearUuid, etcUser, appName, gearName, nmSpace, null, null, null);
                repo = new ApplicationRepository(container);

                if (Prereceive)
                {
                    Dictionary<string, object> options = new Dictionary<string, object>();
                    options["init"] = Init;
                    options["hotDeploy"] = true;
                    options["forceCleanBuild"] = true;
                    options["ref"] = container.DetermineDeploymentRef();
                    container.PreReceive(options);
                }
                else if (Postreceive)
                {
                    RubyHash options = new RubyHash();
                    options["init"] = Init;
                    options["all"] = true;
                    options["reportDeployment"] = true;
                    options["ref"] = container.DetermineDeploymentRef();
                    container.PostReceive(options);
                }
                else if (Build)
                {
                    throw new NotImplementedException();
                }
                else if (Prepare)
                {
                    throw new NotImplementedException();
                }
                else if (Deploy)
                {
                    if (Environment.GetEnvironmentVariable("OPENSHIFT_DEPLOYMENT_TYPE") == "binary")
                    {
                        throw new Exception("OPENSHIFT_DEPLOYMENT_TYPE is 'binary' - git-based deployments are disabled.");
                    }
                    string refToDeploy = container.DetermineDeploymentRef(this.DeployRefId);
                    if (!ValidGitRef(refToDeploy))
                    {
                        throw new Exception("Git ref " + refToDeploy + " is not valid");
                    }
                    RubyHash options = new RubyHash();
                    options["hot_deploy"] = this.HotDeploy;
                    options["force_clean_build"] = this.ForceCleanBuild;
                    options["ref"] = this.DeployRefId;
                    options["report_deployments"] = true;
                    options["all"] = true;
                    container.Deploy(options);
                }
                else if (Activate)
                {
                    status.Output = container.Activate(new RubyHash
                    {
                        {"deployment_id", this.DeploymentId},
                        {"post_install", this.PostInstall.ToBool()},
                        {"all", this.All.ToBool()},
                        {"rotate", this.Rotation && !this.NoRotation},
                        {"report_deployments", true},
                        {"out", !this.AsJson.ToBool()}
                    });
                }
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running gear command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = string.Format("{0}", ex.Message, ex.StackTrace);
                status.ExitCode = 255;
            }

            return status;
        }

        private bool ValidGitRef(string refId)
        {
            ProcessStartInfo pi = new ProcessStartInfo();
            pi.WorkingDirectory = repo.RepositoryPath;
            pi.FileName = Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], @"bin\git.exe");
            pi.Arguments = string.Format("rev-parse --quiet --verify {0}", refId);
            pi.WindowStyle = ProcessWindowStyle.Hidden;
            Process p = Process.Start(pi);
            p.WaitForExit();
            if (p.ExitCode == 0)
                return true;
            else
                return false;
        }
    }
}
