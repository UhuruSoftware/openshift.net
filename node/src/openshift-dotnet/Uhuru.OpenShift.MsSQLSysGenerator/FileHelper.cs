using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.MsSQLSysGenerator
{
    public class FileHelper
    {
        int maxbytes = 0;
        int copied = 0;
        int total = 0;
        public void Copy1(string sourceDirectory, string targetDirectory)
        {

            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);
            //Gets size of all files present in source folder.
            GetSize(diSource, diTarget);
            maxbytes = maxbytes / 1024;

            
            CopyAll(diSource, diTarget);
        }
        private void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {

            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }
            foreach (FileInfo fi in source.GetFiles())
            {

                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);

                total += (int)fi.Length;

                copied += (int)fi.Length;
                copied /= 1024;

                Output.WriteInfo((total / 1048576).ToString() + "MB of " + (maxbytes / 1024).ToString() + "MB copied");

            }
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {



                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        private void GetSize(DirectoryInfo source, DirectoryInfo target)
        {


            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }
            foreach (FileInfo fi in source.GetFiles())
            {
                maxbytes += (int)fi.Length;//Size of File


            }
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                GetSize(diSourceSubDir, nextTargetSubDir);

            }

        }
    }
}
