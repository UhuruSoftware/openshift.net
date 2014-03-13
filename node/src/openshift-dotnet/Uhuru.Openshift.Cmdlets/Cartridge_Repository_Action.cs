using System;
using System.Management.Automation;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("Cartridge", "Repository-Action")]
    public class Cartridge_Repository_Action : System.Management.Automation.Cmdlet 
    {

        [Parameter]
        public string Action;

        [Parameter]
        public string Path;

        [Parameter]
        public string Name;

        [Parameter]
        public string Version;

        [Parameter]
        public string CartridgeVersion;

        protected override void ProcessRecord()
        {
            WriteObject(Execute());
        }

        public ReturnStatus Execute()
        {
            ReturnStatus returnStatus = new ReturnStatus();

            try
            {
                returnStatus.ExitCode = 0;
                switch (Action.ToLower())
                {
                    case "install":
                        {
                            Logger.Debug("Cartridge repository action install");
                            CartridgeRepository.Instance.Install(Path);
                            break;
                        }
                    case "erase":
                        {
                            Logger.Debug("Cartridge repository action erase");
                            CartridgeRepository.Instance.Erase(Name, Version, CartridgeVersion);
                            break;
                        }
                    case "list":
                        {
                            Logger.Debug("Cartridge repository action list");
                            returnStatus.Output = CartridgeRepository.Instance.ToString();
                            break;
                        }
                    default:
                        {
                            returnStatus.Output = string.Format("{0} is not implemented. openshift.ddl may be out of date", Action);
                            returnStatus.ExitCode = 2;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error running cartridge-repository-actions command: {0} - {1}", ex.Message, ex.StackTrace);
                returnStatus.Output = string.Format("{0} failed for {1} {2}", Action, Path, ex.Message);
                returnStatus.ExitCode = 4;
            }

            return returnStatus;
        }
    }
}
