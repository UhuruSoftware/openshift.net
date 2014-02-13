using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Utilities;

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

        public int Start()
        {
            return PRunner(gear =>
            {
                gear.StartGear(new RubyHash());
            });
        }

        public int Stop(bool force = false)
        {
            return PRunner(gear =>
            {
                RubyHash options = new RubyHash()
                {
                    { "user_initiated", "false" },
                };

                if (force)
                {
                    options["force"] = true;
                    options["term_delay"] = 10;
                }

                gear.StopGear(options);
            });
        }

        public int Restart()
        {
            return PRunner(gear =>
            {
                gear.StopGear(new RubyHash());
                gear.StartGear(new RubyHash());
            });                  
        }

        public int Status()
        {
            return PRunner(gear =>
            {
                Console.WriteLine("Checking application {0} ({1}) status:", gear.ContainerName, gear.Uuid);
                Console.WriteLine("-----------------------------------------------");
                if (gear.StopLock)
                {
                    Console.WriteLine("Gear {0} is locked.", gear.Uuid);
                    Console.WriteLine("");
                }

                try
                {
                    gear.Cartridge.EachCartridge(cart =>
                    {
                        Console.WriteLine("Cartridge: {0}...", cart.Name);
                        string output = gear.GetStatus(cart.Name);
                        output = Regex.Replace(output, @"^ATTR:.*$", string.Empty, RegexOptions.Multiline);
                        output = Regex.Replace(output, @"^CLIENT_RESULT:\s+", string.Empty, RegexOptions.Multiline);
                        output = output.Trim();
                        Console.WriteLine(output);
                        Console.WriteLine("");
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Gear {0} Exception: {1}", gear.ContainerName, ex.Message);
                    Console.WriteLine("Gear {0} StackTrace: {1}", gear.ContainerName, ex.Message);
                }

                Console.WriteLine("");
            }, false);
        }

        public int Idle()
        {
            return PRunner(gear =>
            {
                // TODO: vladi: implement idle gear
                // gear.IdleGear
            }, false);
        }

        public int Unidle()
        {
            return PRunner(gear =>
                {
                    // TODO: vladi: implement unidle gear
                    // gear.UnidleGear
                }, false);
        }

        public int PRunner(Action<ApplicationContainer> action, bool skipStopped = true)
        {
            int retCode = 0;
            List<Task> tasks = new List<Task>();

            foreach (ApplicationContainer gear in Gears(skipStopped))
            {
                tasks.Add(Task.Factory.StartNew(() => 
                    {
                        if (action != null)
                        {
                            try
                            {
                                action.Invoke(gear);
                            }
                            catch (Exception ex)
                            {
                                retCode = 1;
                                Logger.Error("Error running parallel gear action ({0}): {1} - {2}", 
                                    gear.Uuid, ex.Message, ex.StackTrace);
                            }
                        }
                    }));
            }

            Task.WaitAll(tasks.ToArray());

            return retCode;
        }

        public IEnumerable<string> GearUuids(bool skipStopped = true)
        {
            foreach (ApplicationContainer gear in Gears(skipStopped))
            {
                yield return gear.Uuid;
            }
        }

        public IEnumerable<ApplicationContainer> Gears(bool skipStopped = true)
        {
            
            List<ApplicationContainer> gearSet = new List<ApplicationContainer>();
            if (this.Uuids != null)
            {
                foreach(string uuid in Uuids)
                {
                    ApplicationContainer gear = ApplicationContainer.GetFromUuid(uuid);
                    // TODO check gear stoplock
                    if (skipStopped && gear.StopLock)
                    {
                        throw new InvalidOperationException(string.Format("Gear is locked: {0}"));
                    }

                    gearSet.Add(gear);
                }
            }
            else
            {
                gearSet = ApplicationContainer.All(null, false).ToList();
            }

            foreach(ApplicationContainer gear in gearSet)
            {
                try
                {
                    // TODO check stop lock again
                    if (skipStopped && gear.StopLock)
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Gear evaluation failed for: {0}", gear.Uuid);
                    Logger.Error("Exception: {0} - {1}", ex.Message, ex.StackTrace);
                }

                yield return gear;
            }
        }
    }
}

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Admin-Ctl-Gears")]
    public class OO_Admin_Ctl_Gears : System.Management.Automation.Cmdlet
    {
        [Parameter(Position = 1, Mandatory = true)]
        [ValidateSet("startall", "stopall", "forcestopall", "restartall", "condrestartall", "waited-startall", "status", "startgear", "stopgear", "forcestopgear", "restartgear", "statusgear", "idlegear", "unidlegear", "list", "listidle")]
        public string Operation;

        [Parameter(Position = 2)]
        public string[] UUID;

        // TODO need lockFile ?
        private const string lockFile = "";

        protected override void ProcessRecord()
        {
            int exitval = 0;
            try
            {
                switch (Operation)
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
                            if (UUID == null || UUID.Length == 0)
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
                            foreach (string uuid in new AdminGearsControl().GearUuids(false))
                            {
                                Console.WriteLine(uuid);
                            }
                        }
                        break;
                    case "listidle":
                        {
                            foreach (string uuid in new AdminGearsControl().GearUuids(false))
                            {
                                // TODO: vladi: determine if apps are idle   
                            }
                        }
                        break;
                    default:
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
