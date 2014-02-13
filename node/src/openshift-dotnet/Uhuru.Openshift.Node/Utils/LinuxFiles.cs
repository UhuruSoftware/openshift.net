using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Runtime.Utils
{
    public static class LinuxFiles
    {
        const string DummySymlinkSuffix = ".LINUXSYMLINK";

        private static string BashBinary
        {
            get
            {
                return Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], @"bin\bash.exe");
            }
        }

        private static string CygpathBinary
        {
            get
            {
                return Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], @"bin\cygpath.exe");
            }
        }

        public static string Cygpath(string directory, bool toWindows = false)
        {
            if (toWindows)
            {
                return ProcessExtensions.RunCommandAndGetOutput(CygpathBinary, string.Format("-w {0}", directory)).StdOut.Trim();
            }
            else
            {
                return ProcessExtensions.RunCommandAndGetOutput(CygpathBinary, directory).StdOut.Trim();
            }
        }

        public static void FixSymlinks(string directory)
        {
            string linuxDir = Cygpath(directory);

            // We use a .LINUXSYMLINK dummy suffix for cygpath when getting symlink names, otherwise it will convert them to the target directory
            string symlinkArguments = string.Format(@"--norc --login -c ""find -L {0} -xtype l -print0 | sort -z | xargs -0 -I {{}} cygpath --windows {{}}{1}""", linuxDir, DummySymlinkSuffix);
            string targetArguments = string.Format(@"--norc --login -c ""find -L {0} -xtype l -print0 | sort -z | xargs -0 -I {{}} cygpath --windows {{}}""", linuxDir);

            string[] symlinks = ProcessExtensions.RunCommandAndGetOutput(BashBinary, symlinkArguments).StdOut.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string[] targets = ProcessExtensions.RunCommandAndGetOutput(BashBinary, targetArguments).StdOut.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (symlinks.Length != targets.Length)
            {
                Logger.Error("Symlink count doesn't match target count in directory '{0}'. Symlinks: {1}. Targets: {2}", directory, string.Join(";", symlinks), string.Join(";", targets));
                throw new Exception("Symlink count doesn't match target count in directory.");
            }

            for (int i = 0; i < symlinks.Length; i++)
            {
                string symlink = symlinks[i].Replace(DummySymlinkSuffix, "");
                string target = targets[i];

                // Only fix links that cygwin translates (it will not translate Windows junction points, but it will see them as symlinks)
                if (symlink != target)
                {
                    Logger.Debug("Fixing symlink {0} -> {1}", symlink, target);
                    File.Delete(symlink);
                    DirectoryUtil.CreateSymLink(symlink, target, DirectoryUtil.SymbolicLink.Directory);
                }
            }
        }

        public static void TakeOwnership(string directory, string windowsUser)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            DirectoryInfo dirInfo = new DirectoryInfo(directory);
            DirectorySecurity dirSecurity = dirInfo.GetAccessControl();

            dirSecurity.SetOwner(new NTAccount(windowsUser));
            dirSecurity.SetAccessRule(
                new FileSystemAccessRule(
                    windowsUser,
                    FileSystemRights.Write | FileSystemRights.Read | FileSystemRights.Delete | FileSystemRights.Modify | FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None | PropagationFlags.InheritOnly,
                    AccessControlType.Allow));

            using (new ProcessPrivileges.PrivilegeEnabler(Process.GetCurrentProcess(), ProcessPrivileges.Privilege.Restore))
            {
                dirInfo.SetAccessControl(dirSecurity);
            }
        }

        public static void TakeOwnershipOfGearHome(string gearHome, string prisonUser)
        {
            Logger.Debug("Setting ownership and acls for gear {0}", gearHome);

            string[] userDirectories = Directory.GetDirectories(gearHome);

            foreach (string dir in userDirectories)
            {
                if (new string[] { ".ssh" }.Contains(Path.GetFileName(dir)))
                {
                    continue;
                }

                try
                {
                    LinuxFiles.TakeOwnership(dir, prisonUser);
                }
                catch (Exception ex)
                {
                    Logger.Error("There was an error while trying to take ownership for files in gear {0}: {1} - {2}", gearHome, ex.Message, ex.StackTrace);
                }
            }
        }
    }
}
