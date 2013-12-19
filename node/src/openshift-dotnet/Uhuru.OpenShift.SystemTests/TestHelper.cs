using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.OpenShift.SystemTests
{
    public class TestHelper
    {
        public static string RunRHC(string command, bool setup)
        {
            string output = string.Empty;
            using (Process p = new Process())
            {
                ProcessStartInfo info = new ProcessStartInfo(@"ruby");
                info.Arguments = String.Format(@"{0}\rhc {1}", ConfigurationManager.AppSettings.Get("RubyPath"), command);
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                info.UseShellExecute = false;
                p.StartInfo = info;
                p.Start();
                // for running RHC setup, some interactions with the process are needed
                if (setup && p != null)
                {
                    p.StandardInput.WriteLine("yes");
                    p.StandardInput.Flush();
                    p.StandardInput.WriteLine("\r\n");
                    p.StandardInput.Flush();
                    p.StandardInput.Close();
                }
                
                output = p.StandardOutput.ReadToEnd();
            }

            return output;
        }

        public static bool SetupRHC()
        {
            string rhcConfig = Path.GetFullPath(ConfigurationManager.AppSettings.Get("RHCConfigFile"));

            string output = TestHelper.RunRHC(String.Format("setup -l {0} -p {1} --create-token --config {2} ", ConfigurationManager.AppSettings.Get("RHCPassword"), ConfigurationManager.AppSettings.Get("RHCUsername"), rhcConfig), true);
            
            if (output.Contains("Your client tools are now configured."))
            {
                File.Copy(rhcConfig, String.Format("{0}\\.openshift\\express.conf", Environment.GetEnvironmentVariable("HOMEPATH")), true);

                return true;
            }

            return false;
        }

        public static string ProcessOuput(string output)
        {

            return "";
        }
    }
}
