using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Runtime
{
    public class ApplicationRepository
    {
        const string GIT = @"""C:\cygwin\installation\bin\git.exe""";
        const string TAR = @"""C:\cygwin\installation\bin\tar.exe""";

        private const string GIT_INIT = @"{0} init
{0} config user.email ""builder@example.com""
{0} config user.name ""Template builder""
{0} config core.logAllRefUpdates true
{0} add -f .
{0} commit -a -m ""Creating template";

        private const string GIT_INIT_BARE = @"{0} init --bare
{0} config core.logAllRefUpdates true";

        private const string GIT_LOCAL_CLONE = @"{0} clone --bare --no-hardlinks template {1}.git
set GIT_DIR=./{1}.git
{0} config core.logAllRefUpdates true
{0} repack";

        private const string GIT_ARHIVE = @"{0} archive --format=tar {1} | (cd {2} & {3} --warning=no-timestamp -xf -)";


        public ApplicationContainer Container { get; set; }
        public string RepositoryPath { get; set; }

        public ApplicationRepository(ApplicationContainer container) : this(container, null) { }

        public ApplicationRepository(ApplicationContainer container, string path)
        {
            this.Container = container;
            this.RepositoryPath = path ?? Path.Combine(container.ContainerDir, "git", string.Format("{0}.git", container.ApplicationName));
        }

        public string PopulateFromCartridge(string cartridgeName)
        {
            if (Exists())
                return null;

            Directory.CreateDirectory(Path.Combine(this.Container.ContainerDir, "git"));

            string[] locations = new string[] {
                Path.Combine(this.Container.ContainerDir, cartridgeName, "template"),
                Path.Combine(this.Container.ContainerDir, cartridgeName, "template.git"),
                Path.Combine(this.Container.ContainerDir, cartridgeName, "usr", "template"),
                Path.Combine(this.Container.ContainerDir, cartridgeName, "usr", "template.git")
            };

            string template = null;
            foreach (string dir in locations)
            {
                if (Directory.Exists(dir))
                {
                    template = dir;
                    break;
                }
            }
            if (template == null)
            {
                return null;
            }

            if (template.EndsWith(".git"))
            {
                DirectoryUtil.DirectoryCopy(template, RepositoryPath, true);
            }
            else
            {
                BuildBare(template);
            }

            Configure();
            return template;
        }

        private void BuildBare(string path)
        {
            string template = Path.Combine(this.Container.ContainerDir, "git", "template");            
            if (Directory.Exists(template))
            {
                Directory.Delete(template, true);                
            }
            Directory.CreateDirectory(template);
            string gitPath = Path.Combine(this.Container.ContainerDir, "git");
            DirectoryUtil.DirectoryCopy(path, gitPath, true);
            RunCmd(string.Format(GIT_INIT, GIT), template);
            RunCmd(string.Format(GIT_LOCAL_CLONE, GIT, this.Container.ApplicationName), gitPath);
        }

        public void Configure()
        {
            
        }

        public void Archive(string destination, string refId)
        {            
            if (!Exists())
                return;
            if (Directory.GetFiles(RepositoryPath).Length == 0)
            {
                return;
            }
            Directory.CreateDirectory(destination);
            string command = string.Format(GIT_ARHIVE, GIT, refId, destination, TAR);
            RunCmd(command, RepositoryPath);
        }

        public bool Exists()
        {
            return Directory.Exists(this.RepositoryPath);
        }

        private void RunCmd(string cmd, string dir)
        {
            string tempfile = Path.GetTempFileName() + ".bat";
            File.WriteAllText(tempfile, cmd);
            ProcessStartInfo pi = new ProcessStartInfo();
            pi.WorkingDirectory = dir;
            pi.UseShellExecute = false;           
            pi.RedirectStandardError = true;
            pi.RedirectStandardOutput = true;
            pi.FileName = "cmd.exe";
            pi.Arguments = "/c " + tempfile;
            Process p = Process.Start(pi);
            p.WaitForExit(30000);
            File.Delete(tempfile);
        }
    }
}
