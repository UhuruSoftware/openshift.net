using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime
{
    public class ApplicationRepository
    {
        const string GIT = @"""C:\Program Files (x86)\Git\bin\git.exe""";

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
        public ApplicationContainer Container { get; set; }

        public ApplicationRepository(ApplicationContainer container)
        {
            this.Container = container;
        }

        public string PopulateFromCartridge(string cartridgeName)
        {
            BuildBare(@"C:\openshift\cartridges\openshift-origin-cartridge-dotnet");
            return string.Empty;
        }

        private void BuildBare(string path)
        {
            string template = Path.Combine(this.Container.ContainerDir, "git", "template");            
            if (Directory.Exists(template))
                Directory.Delete(template, true);
            Directory.CreateDirectory(template);
            string gitPath = Path.Combine(this.Container.ContainerDir, "git");

            DirectoryCopy(path, gitPath, true);

            RunCmd(string.Format(GIT_INIT, GIT), template);
            RunCmd(string.Format(GIT_LOCAL_CLONE, GIT, this.Container.ApplicationName), gitPath);
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

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
