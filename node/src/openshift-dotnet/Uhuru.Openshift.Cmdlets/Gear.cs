using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Config;

namespace Uhuru.Openshift.Cmdlets
{
    [Alias("Gear")]
    [Cmdlet("OO", "Gear")]
    public class Gear : Cmdlet
    {
        [Parameter(HelpMessage = "Proceed even if the git ref to deploy isn't found.", ParameterSetName = "Prereceive")]
        [Parameter(ParameterSetName = "Postreceive")]
        public SwitchParameter Init { get; set; }
        
        [Parameter(HelpMessage = "Run the git prereceive steps", ParameterSetName = "Prereceive" )]
        public SwitchParameter Prereceive { get; set; }

        [Parameter(HelpMessage = "Run the git postreceive steps", ParameterSetName = "Postreceive" )]
        public SwitchParameter Postreceive { get; set; }

        [Parameter(HelpMessage = "Run the build steps", ParameterSetName="Build"  )]
        public SwitchParameter Build { get; set; }

        [Parameter(HelpMessage = "Prepare a binary deployment artifact for distribution and activation", ParameterSetName="Prepere", Position=0)]
        public SwitchParameter Prepare { get; set; }

        [Parameter(HelpMessage="Prepare a binary deployment artifact for distribution and activation", ParameterSetName="Prepere", Position=1)]
        public string File { get; set; }

        [Parameter(HelpMessage = "Distribute a build", ParameterSetName = "Distribute" )]
        public SwitchParameter Distribute { get; set; }

        [Parameter(HelpMessage = "Deployment ID", ParameterSetName = "Distribute")]
        [Parameter(ParameterSetName = "ArchiveDeployment")]
        public string DeploymentId { get; set; }

        [Parameter(HelpMessage = "Activate a build", ParameterSetName = "Activate", Position=0)]
        public SwitchParameter Activate { get; set; }

        [Parameter(HelpMessage= "Run post_install for new gears", ParameterSetName= "Activate")]
        public SwitchParameter PostInstall { get; set; }

        [Parameter(HelpMessage = "Render the results as JSON to stdout", ParameterSetName = "Activate")]
        [Parameter(ParameterSetName="Restart")]
        [Parameter(ParameterSetName = "RotateOut")]
        [Parameter(ParameterSetName = "RotateIn")]
        public SwitchParameter AsJson { get; set; }

        [Parameter(HelpMessage = "Rotate gears out/in (defaults to true)", ParameterSetName = "Activate")]
        public SwitchParameter Rotation { get; set; }

        [Parameter(HelpMessage = "Archive the current deployment", ParameterSetName = "ArchiveDeployment" )]
        public SwitchParameter ArchiveDeployment { get; set; }

        [Parameter(HelpMessage = "Create a deployment directory. Should only be used by CI builders", ParameterSetName = "CreateDeploymentDir")]
        public SwitchParameter CreateDeploymentDir { get; set; }

        [Parameter(HelpMessage = "Run the remotedeploy steps", ParameterSetName = "RemoteDeploy" )]
        public SwitchParameter RemoteDeploy { get; set; }

        [Parameter(HelpMessage = "Datetime to deploy", ParameterSetName = "RemoteDeploy")]
        public string DeploymentDateTimeName { get; set; }

        [Parameter(HelpMessage = "Run the deploy steps", ParameterSetName = "Deploy" )]
        public SwitchParameter Deploy { get; set; }

        [Parameter(HelpMessage = "Perform hot deployment", ParameterSetName = "Deploy")]
        public SwitchParameter HotDeploy { get; set; }

        [Parameter(HelpMessage = "Perform a clean build", ParameterSetName = "Deploy")]
        public SwitchParameter ForceCleanBuild { get; set; }

        [Parameter(HelpMessage = "Deploy Reference ID", ParameterSetName = "Deploy")]
        public string DeployRefId { get; set; }

        [Parameter(HelpMessage = "Deploy a binary artifact", ParameterSetName = "BinaryDeploy" )]
        public SwitchParameter BinaryDeploy { get; set; }

        [Parameter(HelpMessage = "List the gear's deployments", ParameterSetName = "Deployments" )]
        public SwitchParameter Deployments { get; set; }

        [Parameter(HelpMessage = "The cart to use", ParameterSetName = "Start")]
        [Parameter(ParameterSetName = "Stop")]
        [Parameter(ParameterSetName = "Restart")]
        [Parameter(ParameterSetName = "Reload")]
        [Parameter(ParameterSetName = "Status")]
        [Parameter(ParameterSetName = "RotateOut")]
        [Parameter(ParameterSetName = "RotateIn")]
        public string Cart { get; set; }

        [Parameter(HelpMessage = "Start the gear/cart", ParameterSetName = "Start")]
        public SwitchParameter Start { get; set; }

        [Parameter(HelpMessage = "Stop the gear/cart", ParameterSetName = "Stop")]
        public SwitchParameter Stop { get; set; }

        [Parameter(HelpMessage = "Skip the gear stop if the hot deploy marker is present in the application Git repo in the commit specified by --git-ref", ParameterSetName = "Stop")]
        public SwitchParameter Conditional { get; set; }

        [Parameter(HelpMessage = "The git ref to use when checking for the presence of the hot deploy marker file", ParameterSetName = "Stop")]
        public string GitRef { get; set; }

        [Parameter(HelpMessage = "Skip stopping the web proxy, if it exists", ParameterSetName = "Stop")]
        public SwitchParameter ExclusiveWebProxy { get; set; }

        [Parameter(HelpMessage = "Restart a cart", ParameterSetName = "Restart")]
        public SwitchParameter Restart { get; set; }

        [Parameter(HelpMessage = "Restart all instances of the specified cartridge for all gears for this application", ParameterSetName = "Restart")]
        public SwitchParameter All { get; set; }

        [Parameter(HelpMessage = "Reload a cart", ParameterSetName = "Reload")]
        public SwitchParameter Reload { get; set; }

        [Parameter(HelpMessage = "Get the status for a cart", ParameterSetName = "Status")]
        public SwitchParameter Status { get; set; }

        [Parameter(HelpMessage = "Snapshot an application", ParameterSetName = "Snapshot")]
        public SwitchParameter Snapshot { get; set; }

        [Parameter(HelpMessage = "Restore an application", ParameterSetName = "Restore")]
        public SwitchParameter Restore { get; set; }

        [Parameter(HelpMessage = "Rebuild the application as part of restoration", ParameterSetName = "Restore")]
        public SwitchParameter RestoreGitRepo { get; set; }

        [Parameter(HelpMessage = "Do not report a deployment after restoration", ParameterSetName = "Restore")]
        public SwitchParameter NoReportDeployments { get; set; }

        [Parameter(HelpMessage = "Disables this gear from receiving traffic from the proxy", ParameterSetName = "RotateOut")]
        public SwitchParameter RotateOut { get; set; }

        [Parameter(HelpMessage = "Store the disabling of this gear in the proxy configuration file", ParameterSetName = "RotateOut")]
        [Parameter(ParameterSetName="RotateIn")]
        public SwitchParameter Persist { get; set; }

        [Parameter(HelpMessage = "UUID of the gear to disable", ParameterSetName = "RotateOut")]
        [Parameter(ParameterSetName = "RotateIn")]
        public string GearUUID { get; set; }

        [Parameter(HelpMessage = "Enables this gear to receive traffic from the proxy", ParameterSetName = "RotateIn")]
        public SwitchParameter RotateIn { get; set; }

        ApplicationContainer container = null;
        ApplicationRepository repo = null;

        protected override void ProcessRecord()
        {
            ReturnStatus status = new ReturnStatus();
            try
            {
                string appUuid = Environment.GetEnvironmentVariable("OPENSHIFT_APP_UUID");
                string gearUuid = Environment.GetEnvironmentVariable("OPENSHIFT_GEAR_UUID");
                string appName = Environment.GetEnvironmentVariable("OPENSHIFT_APP_NAME");
                string gearName = Environment.GetEnvironmentVariable("OPENSHIFT_GEAR_NAME");
                string nmSpace = Environment.GetEnvironmentVariable("OPENSHIFT_NAMESPACE");

                container = new ApplicationContainer(appUuid, gearUuid, System.Security.Principal.WindowsIdentity.GetCurrent().Name, appName, gearName, nmSpace, null, null, null);
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
                    Dictionary<string, object> options = new Dictionary<string, object>();
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
                    Dictionary<string, object> options = new Dictionary<string,object>();
                    options["hot_deploy"] = this.HotDeploy;
                    options["force_clean_build"] = this.ForceCleanBuild;
                    options["ref"] = this.DeployRefId;
                    options["report_deployments"] = true;
                    options["all"] = true;
                    container.Deploy(options);
                }
                status.ExitCode = 0;
            }
            catch (Exception ex)
            {
                status.Output = ex.Message;
                status.ExitCode = 255;
            }
            this.WriteObject(status);
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
