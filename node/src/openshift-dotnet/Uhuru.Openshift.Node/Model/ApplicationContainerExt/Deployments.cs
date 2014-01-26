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
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Runtime
{
    public partial class ApplicationContainer
    {
        const string DEPLOYMENT_DATETIME_FORMAT = "yyyy-MM-dd_HH-mm-s";

        public string DetermineDeploymentRef(string input=null)
        {
            string refId = input;
            if(string.IsNullOrEmpty(refId))
            {
                if (Environment.GetEnvironmentVariables().Contains("OPENSHIFT_DEPLOYMENT_BRANCH"))
                {
                    refId = Environment.GetEnvironmentVariable("OPENSHIFT_DEPLOYMENT_BRANCH");
                }
                else
                {
                    refId = "master";
                }
            }
            return refId;
        }

        public string GetDeploymentDateTimeForDeploymentId(string deploymentId)        
        {
            if (!DeploymentExists(deploymentId))
            {
                return null;
            }

            string symlinkTarget = Path.Combine(this.ContainerDir, "app-deployments", "by-id", deploymentId);

            string datetime = Path.GetFileName(FileUtil.GetSymlinkTargetLocation(symlinkTarget));

            Logger.Debug("Deployment datetime (symlink) for {0} is {1}", symlinkTarget, datetime);

            return datetime;
        }

        public bool DeploymentExists(string deploymentId)
        {
            return Directory.Exists(Path.Combine(this.ContainerDir, "app-deployments", "by-id", deploymentId));
        }

        public List<string> AllDeployments()
        {
            string deploymentsDir = Path.Combine(this.ContainerDir, "app-deployments");
            return Directory.GetDirectories(deploymentsDir).Where(d => !new string[] { "by-id", "current" }.Contains(new DirectoryInfo(d).Name)).ToList<string>();
        }

        public List<string> AllDeploymentsByActivation()
        {
            List<string> deployments = AllDeployments();
            int count = 0;
            deployments.Sort();
            deployments.OrderBy(d =>
                {
                    DeploymentMetadata metadata = DeploymentMetadataFor(new DirectoryInfo(d).Name);
                    float latestActivation = 0;
                    if (metadata.Activations.Count > 0)
                    {
                        latestActivation = metadata.Activations.Last();
                    }
                    count++;
                    if (latestActivation != 0)
                    {
                        return latestActivation;
                    }
                    else
                    {
                        return float.MaxValue - count;
                    }
                });
            return deployments;
        }

        public DeploymentMetadata DeploymentMetadataFor(string deploymentDateTime)
        {
            return new DeploymentMetadata(this, deploymentDateTime);
        }

        public string LatestDeploymentDateTime()
        {
            string latest = AllDeploymentsByActivation().Last();
            return new DirectoryInfo(latest).Name;
        }

        public string CalculateDeploymentId()
        {
            return Guid.NewGuid().ToString().Substring(0, 8);
        }

        public void LinkDeploymentId(string deploymentDateTime, string deploymentId)
        {
            Logger.Debug("Linking deployment id {0} to datetime {1}, for gear {2}", deploymentId, deploymentDateTime, this.Uuid);

            string target = Path.Combine(this.ContainerDir, "app-deployments", deploymentDateTime);
            string link = Path.Combine(this.ContainerDir, "app-deployments", "by-id", deploymentId);
            DirectoryUtil.CreateSymbolicLink(link, target, DirectoryUtil.SymbolicLink.Directory);
        }

        public void UnlinkDeploymentId(string deploymentId)
        {
            Directory.Delete(Path.Combine(this.ContainerDir, "app-deployments", "by-id", deploymentId), true);
        }

        public void SyncRuntimeRepoDirToDeployment(string deploymentDateTime)
        {
            SyncRuntimeDirToDeployment(deploymentDateTime, "repo");
        }

        public void SyncRuntimeDependenciesDirToDeployment(string deploymentDateTime)
        {
            SyncRuntimeDirToDeployment(deploymentDateTime, "dependencies");
        }

        public void SyncRuntimeBuildDependenciesDirToDeployment(string deploymentDateTime)
        {
            SyncRuntimeDirToDeployment(deploymentDateTime, "build-dependencies");
        }

        public void SyncRuntimeDirToDeployment(string deploymentDateTime, string name)
        {
            string from = Path.Combine(this.ContainerDir, "app-root", "runtime", name);
            string to = Path.Combine(this.ContainerDir, "app-deployments", deploymentDateTime, name);
            SyncFiles(from, to);
        }

        public string CalculateDeploymentChecksum(string deploymentId)
        {
            string deploymentDir = Path.Combine(this.ContainerDir, "app-deployments", "by-id", deploymentId);
            string binPath = Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], "bin");
            string command = string.Format(@"{0}\tar.exe -c --exclude metadata.json . | {0}\tar.exe -xO | {0}\sha1sum.exe | {0}\cut.exe -f 1 -d ' '", binPath);
            return RunProcessInContainerContext(deploymentDir, command).StdOut.Trim();
        }

        public void UpdateCurrentDeploymentDateTimeSymlink(string deploymentDateTime)
        {
            string file = Path.Combine(this.ContainerDir, "app-deployments", "current");
            Directory.Delete(file);
            DirectoryUtil.CreateSymbolicLink(file, deploymentDateTime, DirectoryUtil.SymbolicLink.Directory);
        }

        public void SyncDeploymentRepoDirToRuntime(string deploymentDateTime)
        {
            SyncDeploymentDirToRuntime(deploymentDateTime, "repo");
        }

        public void SyncDeploymentDependenciesDirToRuntime(string deploymentDateTime)
        {
            SyncDeploymentDirToRuntime(deploymentDateTime, "dependencies");
        }

        public void SyncDeploymentBuildDependenciesDirToRuntime(string deploymentDateTime)
        {
            SyncDeploymentDirToRuntime(deploymentDateTime, "build-dependencies");
        }

        public void SyncDeploymentDirToRuntime(string deploymentDateTime, string name)
        {
            string to = Path.Combine(this.ContainerDir, "app-root", "runtime", name);
            string from = Path.Combine(this.ContainerDir, "app-deployments", deploymentDateTime, name);
            SyncFiles(from, to);
        }

        public void SyncFiles(string from, string to)
        {
            // TODO use rsync
            DirectoryUtil.DirectoryCopy(from, to, true);
        }

        private DateTime CreateDeploymentDir()
        {
            DateTime deploymentdateTime = DateTime.Now;

            string fullPath = Path.Combine(this.ContainerDir, "app-deployments", deploymentdateTime.ToString(DEPLOYMENT_DATETIME_FORMAT));
            Directory.CreateDirectory(Path.Combine(fullPath, "repo"));
            Directory.CreateDirectory(Path.Combine(fullPath, "dependencies"));
            Directory.CreateDirectory(Path.Combine(fullPath, "build-depedencies"));
            SetRWPermissions(fullPath);
            PruneDeployments();
            return deploymentdateTime;
        }

        public List<RubyHash> CalculateDeployments()
        {
            List<RubyHash> deployments = new List<RubyHash>();
            foreach (string d in AllDeployments())
            {
                string deploymentDateTime = new DirectoryInfo(d).Name;
                DeploymentMetadata deploymentMetadata = DeploymentMetadataFor(deploymentDateTime);
                deployments.Add(new RubyHash() {
                    { "id", deploymentMetadata.Id},
                    {"ref", deploymentMetadata.GitRef},
                    {"sha1",deploymentMetadata.GitSha},
                    {"force_clean_build", deploymentMetadata.ForceCleanBuild},
                    {"hot_deploy", deploymentMetadata.HotDeploy},
                    {"created_at", RubyCompatibility.DateTimeToEpochSeconds(DateTime.Parse(deploymentDateTime))},
                    {"activations", deploymentMetadata.Activations}
                });
            }

            return deployments;
        }

    }
}
