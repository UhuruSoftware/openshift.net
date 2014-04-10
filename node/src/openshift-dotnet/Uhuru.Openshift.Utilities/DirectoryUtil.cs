using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Utilities
{
    public class DirectoryUtil
    {
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
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

        public static void EmptyDirectory(string directory)
        {
            if(!Directory.Exists(directory))
            {
                return;
            }
            DirectoryInfo dir = new DirectoryInfo(directory);
            foreach (FileInfo fi in dir.GetFiles())
            {
                fi.Delete();
            }
            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                di.Delete(true);
            }
        }

        [DllImport("kernel32.dll", SetLastError=true)]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        public enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        public static void CreateSymLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags)
        {
            if (!DirectoryUtil.CreateSymbolicLink(lpSymlinkFileName, lpTargetFileName, dwFlags))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }
}
