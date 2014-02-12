using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Runtime
{
    public class AdminGearsControl
    {
        string[] Uuids;

        public AdminGearsControl()
        {
            new AdminGearsControl(null);
        }

        public AdminGearsControl(string[] containerUuids)
        {
            this.Uuids = containerUuids;

        }

        public string Start()
        {
            throw new NotImplementedException();
        }

        public string Stop(bool force = false)
        {
            throw new NotImplementedException();
        }

        public string Restart()
        {
            throw new NotImplementedException();
        }

        public string Status()
        {
            throw new NotImplementedException();
        }

        public string Idle()
        {
            throw new NotImplementedException();
        }

        public string Unidle()
        {
            throw new NotImplementedException();
        }

        public void PRunner(bool skipStopped = true)
        {
            throw new NotImplementedException();
        }

        public void GearUuids(bool skipStopped = true)
        {
            throw new NotImplementedException();
        }

        public delegate void GearCallback(ApplicationContainer gear);
        public List<ApplicationContainer> Gears(GearCallback action, bool skipStopped = true)
        {
            
            List<ApplicationContainer> gearSet = new List<ApplicationContainer>();
            if (this.Uuids != null)
            {
                foreach(string uuid in Uuids)
                {
                    ApplicationContainer gear = ApplicationContainer.GetFromUuid(uuid);
                    // TODO check gear stoplock
                    gearSet.Add(gear);
                }
            }
            else
            {
                // TODO
                // gearSet = ApplicationContainer.All
            }

            foreach(ApplicationContainer gear in gearSet)
            {
                // TODO check stop lock again
                action(gear);
            }

            return gearSet;
        }
    }
}

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Admin-Ctl-Gears")]
    public class OO_Admin_Ctl_Gears : System.Management.Automation.Cmdlet
    {
        [Parameter(Position = 1, Mandatory = true)]
        [ValidateSet("startall", "stopall", "forcestopall", "status", "restartall", "waited-startall", "condrestartall", "startgear")]
        public string Operation;

        [Parameter(Position=2)]
        public string[] UUID;

        // TODO need lockFile ?
        private const string lockFile = "";

        protected override void ProcessRecord()
        {
            string exitval = string.Empty;
            try
            {
                switch(Operation)
                {
                    case "startall":
                        {
                            // start in new thread 
                            exitval = new AdminGearsControl().Start();
                            break;
                        }
                    case "stopall":
                        {
                            exitval = new AdminGearsControl().Stop();
                            break;
                        }
                    case "forcestopall":
                        {
                            exitval = new AdminGearsControl().Stop(true);
                            break;
                        }
                    case "restartall":
                        {
                            exitval = new AdminGearsControl().Restart();
                            break;
                        }
                    case "condrestartall":
                        {
                            if (File.Exists(lockFile))
                            {
                                exitval = new AdminGearsControl().Restart();
                            }
                            break;
                        }
                    case "waited-startall":
                        {
                            exitval = new AdminGearsControl().Start();
                            break;
                        }
                    case "status":
                        {
                            this.WriteObject("Checking OpenshiftServices: ");
                            this.WriteObject(Environment.NewLine);
                            exitval = new AdminGearsControl().Status();
                            break;
                        }
                    case "startgear":
                        {
                            if(UUID == null || UUID.Length == 0)
                            {
                                throw new Exception("Requires a gear uuid");
                            }
                            exitval = new AdminGearsControl(UUID).Start();
                            break;
                        }
                    case "stopgear":
                        {
                            if (UUID == null || UUID.Length == 0)
                            {
                                throw new Exception("Requires a gear uuid");
                            }
                            exitval = new AdminGearsControl(UUID).Stop();
                            break;
                        }
                    case "forcestopgear":
                        {
                            if (UUID == null || UUID.Length == 0)
                            {
                                throw new Exception("Requires a gear uuid");
                            }
                            exitval = new AdminGearsControl(UUID).Stop();
                            break;
                        }
                    case "restartgear":
                        {
                            if (UUID == null || UUID.Length == 0)
                            {
                                throw new Exception("Requires a gear uuid");
                            }
                            exitval = new AdminGearsControl(UUID).Restart();
                            break;
                        }
                    case "statusgear":
                        {
                            if (UUID == null || UUID.Length == 0)
                            {
                                throw new Exception("Requires a gear uuid");
                            }
                            exitval = new AdminGearsControl(UUID).Status();
                            break;
                        }
                    case "idlegear":
                        {
                            if (UUID == null || UUID.Length == 0)
                            {
                                throw new Exception("Requires a gear uuid");
                            }
                            exitval = new AdminGearsControl(UUID).Idle();
                            break;
                        }
                    case "unidlegear":
                        {
                            if (UUID == null || UUID.Length == 0)
                            {
                                throw new Exception("Requires a gear uuid");
                            }
                            exitval = new AdminGearsControl(UUID).Unidle();
                            break;
                        }
                    case "list":
                        {

                            throw new NotImplementedException();
                        }
                    case "listidle":
                        {
                            throw new NotImplementedException();
                        }
                    default :
                        {
                            WriteObject("Usage: {startall|stopall|forcestopall|status|restartall|waited-startall|condrestartall|startgear [uuid]|stopgear [uuid]|forcestopgear [uuid]|restartgear [uuid]|idlegear [gear]|unidlegear [gear]|list|listidle}");
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                this.WriteObject(ex.ToString());
            }
        }
    }
}
