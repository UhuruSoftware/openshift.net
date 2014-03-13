using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Admin-Cartridge")]
    public class OO_Admin_Cartridge : System.Management.Automation.Cmdlet
    {

        [Parameter]
        public string Action;

        [Parameter]
        public string Source;

        [Parameter]
        public string Name;

        [Parameter]
        public string Version;

        [Parameter]
        public SwitchParameter D;

        [Parameter]
        public string CartridgeVersion;

        [Parameter]
        public SwitchParameter Recursive;

        protected override void ProcessRecord()
        {
            this.WriteObject(Execute());
        }

        public ReturnStatus Execute()
        {
            ReturnStatus returnStatus = new ReturnStatus();

            CartridgeRepository repository = CartridgeRepository.Instance;

            if(string.IsNullOrEmpty(Action))
            {
                returnStatus.Output = "Usage: --action ACTION [--recursive] [--source directory] [--name NAME --version VERSION --cartridge_version VERSION]";
                returnStatus.ExitCode = 1;
                return returnStatus;
            }

            try
            {
                switch (Action.ToLower())
                {
                    case "install":
                        {                            
                            string[] dirs = null;
                            if (Recursive)
                            {
                                Directory.GetDirectories(Source);
                            }
                            else
                            {
                                dirs = new string[] { Source };
                            }

                            bool success = true;

                            foreach (string dir in dirs)
                            {
                                try
                                {
                                    repository.Install(dir);
                                }
                                catch (Exception e)
                                {
                                    success = false;
                                    Console.Error.WriteLine(string.Format("install failed for {0}: {1}", dir, e.Message));
                                    if (D) Console.Error.WriteLine(e.StackTrace);
                                }
                            }

                            if (success)
                                returnStatus.Output = "succeeded";
                            else
                                returnStatus.Output = "installation failed";

                            break;
                        }
                    case "erase":
                        {
                            try
                            {
                                repository.Erase(Name, Version, CartridgeVersion);
                                returnStatus.Output = "succeeded";
                            }
                            catch (KeyNotFoundException e)
                            {
                                returnStatus.Output = "requested cartridge does not exist: " + e.ToString();
                            }
                            catch (Exception e)
                            {
                                returnStatus.Output = "Couldn't erase cartridge: " + e.ToString();
                            }

                            break;
                        }
                    case "list":
                        {                            
                            if (D)
                            {
                                returnStatus.Output = repository.Inspect();
                            }
                            else
                            {
                                returnStatus.Output = repository.ToString();
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
                returnStatus.ExitCode = 0;
            }
            catch (Exception ex)
            {
                returnStatus.ExitCode = 1;
                returnStatus.Output = ex.Message;
                if (D) Console.Error.WriteLine(ex.ToString());
            }

            return returnStatus;
        }
    }
}
