using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
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
            ReturnStatus returnStatus = new ReturnStatus();

            try
            {
                switch (Action.ToLower())
                {
                    case "install":
                        {
                            Logger.Debug("Cartridge repository action install");
                        } break;
                    case "erase":
                        {
                            Logger.Debug("Cartridge repository action erase");
                        } break;
                    case "list":
                        {
                            Logger.Debug("Cartridge repository action list");

                        }break;
                    default:
                        {
                            throw new NotImplementedException(string.Format("{0} is not implemented. openshift.ddl may be out of date"));
                        }
                }
                returnStatus.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error running cartridge-repository-actions command: {0} - {1}", ex.Message, ex.StackTrace);
                returnStatus.Output = ex.ToString();
                returnStatus.ExitCode = 2;
            }

            WriteObject(returnStatus);
        }

    }
}
