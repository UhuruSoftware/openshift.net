using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Utils;
using YamlDotNet.RepresentationModel.Serialization;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Accept-Node")]
    public class OO_Accept_Node : System.Management.Automation.Cmdlet
    {
        [Parameter]
        public decimal Timeout;

        [Parameter]
        public SwitchParameter V;

        [Parameter]
        public SwitchParameter RunUpgradeChecks;

        static string CONF_DIR = @"C:\openshift";
        string NODE_CONF_FILE = Path.Combine(CONF_DIR, "node.conf");
        string RESOURCE_LINITS_FILE = Path.Combine(CONF_DIR, "resource_limits.conf");
        string[] SERVICES = new string[] { "openshift.sshd", "openshift.mcollectived" };

        ReturnStatus status;
        string externalEthDev;
        string gearBaseDir;
        EtcUser[] users;

        protected override void ProcessRecord()
        {
            status = new ReturnStatus();
            status.ExitCode = 0;

            LoadNodeConf();
            FindExtNetDev();
            LoadUsers();

            if(RunUpgradeChecks)
            {
                CheckUpgrades();
            }
            else
            {
                //ValidateEnv();
                CheckNodePublicResolution();
                //CheckSelinux();
                //CheckPackages();
                CheckServices();
                //CheckServiceContexts();
                //CheckSemaphores();
                //CheckCgroupConfig();
                //CheckCgroupProcs();
                //CheckTcConfig();
                //CheckQuotas();
                CheckUsers();
                CheckAppDirs();
                //CheckSystemHttpdConfigs();
                CheckCartridgeRepository();
            }

            this.WriteObject(status);
        }

        private void CheckCartridgeRepository()
        {
            VerboseMessage("checking cartridge repository");

            Dictionary<string, object> dstCTimes = new Dictionary<string, object>();
            Dictionary<string, object> dstManifests = new Dictionary<string, object>();

            foreach (string cart in Directory.GetDirectories(NodeConfig.Values.Get("CARTRIDGE_REPO_DIR")))
            {
                string manifestPath = Path.Combine(cart, "metadata", "manifest.yml");
                if (!File.Exists(manifestPath))
                {
                    continue;
                }
                try
                {                    
                    dstCTimes[manifestPath] = File.GetLastWriteTime(manifestPath);

                    using (var input = new StringReader(manifestPath))
                    {
                        var deserializer = new Deserializer();
                        dstManifests[manifestPath] = (dynamic)deserializer.Deserialize(input);
                    }
                }
                catch(Exception e)
                {
                    DoFail(string.Format("Error with manifest file {0} {1}", manifestPath, e.Message));
                    VerboseMessage(e.ToString());
                    if(dstManifests.ContainsKey(manifestPath))
                    {
                        dstManifests.Remove(manifestPath);
                    }
                }
            }

            foreach(string cart in Directory.GetDirectories(NodeConfig.Values.Get("CARTRIDGE_BASE_PATH")))
            {
                string manifestPath = Path.Combine(cart, "metadata", "manifest.yml");
                if(!File.Exists(manifestPath))
                {
                    continue;
                }
                try
                {                    
                    DateTime srcCt = File.GetLastWriteTime(manifestPath);
                    dynamic srcM = null;

                    using (var input = new StringReader(File.ReadAllText(manifestPath)))
                    {
                        var deserializer = new Deserializer();
                        srcM = (dynamic)deserializer.Deserialize(input);
                    }

                    if (dstManifests.Count > 0)
                    {
                        object matchedCt = null;
                        foreach(KeyValuePair<string, object> pair in dstManifests)
                        {
                            Dictionary<string, object> dstM = (Dictionary<string, object>)pair.Value;
                            if(srcM["Name"] == dstM["Name"] && srcM["Cartridge-Version"] == dstM["Cartridge-Version"])
                            {
                                matchedCt = dstCTimes[pair.Key];
                                break;
                            }
                        }
                    
                        if (matchedCt == null)
                        {
                            DoFail("no manifest in the cart repo matches " + manifestPath);
                        }
                        else if ((DateTime)matchedCt < srcCt)
                        {
                            DoFail("cart repo version is older than " + manifestPath);
                        }
                    }

                }
                catch(Exception e)
                {
                    DoFail(string.Format("Error with manifest file {0} {1}", manifestPath, e.Message));
                    VerboseMessage(e.ToString());
                    if(dstManifests.ContainsKey(manifestPath))
                    {
                        dstManifests.Remove(manifestPath);
                    }
                }
            }

        }

        private void CheckAppDirs()
        {
            VerboseMessage("checking application dirs");

            foreach(string gearHome in Directory.GetDirectories(gearBaseDir))
            {
                foreach(string dotdir in new string[] {".ssh",".env",".sandbox",".tmp"})
                {
                    if(!Directory.Exists(Path.Combine(gearHome, dotdir)))
                    {
                        DoFail(string.Format("Directory {0} doesn't have a {1} directory", gearHome, dotdir));
                    }
                }

                bool foundCartridge = false;
                foreach(string dir in Directory.GetDirectories(gearHome))
                {
                    if(File.Exists(Path.Combine(dir, "metadata", "manifest.yml")))
                    {
                        foundCartridge = true;
                        break;
                    }
                }

                if(!foundCartridge)
                {
                    DoFail(string.Format("directory {0} doesn't have a cartridge directory", gearHome));
                }
                if(!users.Where(u => u.Dir == gearHome).Any())
                {
                    DoFail(string.Format("directory {0} doesn't have an associated user", gearHome));
                }                
            }
        }

        private void CheckUsers()
        {
            VerboseMessage(string.Format("checking {0} user accounts", users.Length));
            foreach(EtcUser user in users)
            {
                if(!Directory.Exists(LinuxFiles.Cygpath(user.Dir, true)))
                {                    
                    DoFail(string.Format("user {0} does not have a home directory {1}", user.Name, user.Dir));
                }                
            }
        }

        private void CheckUpgrades()
        {
            VerboseMessage("running upgrade checks");

            foreach(string gearHome in Directory.GetDirectories(gearBaseDir))
            {
                bool upgradeMarkersFound = false;
                string upgradeDir = Path.Combine(gearHome, "app-root", "runtime", ".upgrade");
                if(Directory.Exists(upgradeDir))
                {
                    upgradeMarkersFound = Directory.EnumerateFileSystemEntries(upgradeDir).Any();
                }

                bool preUpgradeStateFound = File.Exists(Path.Combine(gearHome, "app-root", "runtime", ".preupgrade_state"));

                if(upgradeMarkersFound)
                {
                    DoFail(string.Format("directory {0} contains upgrade data", gearHome));
                }

                if(preUpgradeStateFound)
                {
                    DoFail(string.Format("directory {0} contains pre-upgrade data", gearHome));
                }
            }
        }

        private void LoadUsers()
        {
            users = new Etc(NodeConfig.Values).GetAllUsers();
        }

        private void CheckServices()
        {
            VerboseMessage("checking services");
            foreach (string service in SERVICES)
            {                
                using(ServiceController sc = new ServiceController(service))
                {
                    if(sc.Status != ServiceControllerStatus.Running)
                    {
                        DoFail(string.Format("service {0} not running", service));
                    }
                }

            }
        }

        private void FindExtNetDev()
        {
            NetworkInterface nic = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.Name == externalEthDev).FirstOrDefault();
            if(nic == null)
            {
                DoFail("SEVERE: not a valid ethernet device: " + externalEthDev);
            }
        }

        private void LoadNodeConf()
        {
            VerboseMessage(string.Format("loading node configuration file {0}", NodeConfig.NodeConfigFile));
            string[] values = new string[] {"GEAR_BASE_DIR", 
                "GEAR_SHELL", "GEAR_GECOS",
                //"GEAR_MIN_UID", "GEAR_MAX_UID", 
                "CARTRIDGE_BASE_PATH", "CLOUD_DOMAIN", "BROKER_HOST", "PUBLIC_IP", "PUBLIC_HOSTNAME"};
            foreach(string value in values)
            {
                if(NodeConfig.Values.Get(value) == string.Empty)
                {
                    DoFail(string.Format("SEVERE: in {0}, {1} not defined", NodeConfig.NodeConfigFile, value));
                }
            }

            gearBaseDir = NodeConfig.Values.Get("GEAR_BASE_DIR");
            if(!Directory.Exists(gearBaseDir))
            {
                DoFail(string.Format("GEAR_BASE_DIR does not exist or is not a directory: {0}", gearBaseDir));
            }

            externalEthDev = NodeConfig.Values.Get("EXTERNAL_ETH_DEV") == string.Empty ? "Ethenernet" : NodeConfig.Values.Get("EXTERNAL_ETH_DEV");
            VerboseMessage("loading resource limit file " + RESOURCE_LINITS_FILE);
            if(!File.Exists(RESOURCE_LINITS_FILE))
            {
                DoFail("No resource limits file: " + RESOURCE_LINITS_FILE);
            }
        }

        private void CheckNodePublicResolution()
        {
            try
            {
                VerboseMessage(string.Format("loading node configuration file {0}", NodeConfig.NodeConfigFile));
                string gearBaseDir = NodeConfig.Values["GEAR_BASE_DIR"];

                VerboseMessage(string.Format("checking node public hostname resolution"));

                try
                {
                    bool resolvesOk = Dns.Resolve(NodeConfig.Values["PUBLIC_HOSTNAME"]).AddressList.Select(ip => ip.ToString()).Contains(NodeConfig.Values["PUBLIC_IP"]);

                    if (resolvesOk)
                    {
                        VerboseMessage(string.Format("{0} resolves to {1}", NodeConfig.Values["PUBLIC_HOSTNAME"], NodeConfig.Values["PUBLIC_IP"]));
                    }
                    else
                    {
                        DoFail(string.Format("{0} does not resolve to {1}", NodeConfig.Values["PUBLIC_HOSTNAME"], NodeConfig.Values["PUBLIC_IP"]));
                    }
                }
                catch (SocketException)
                {
                    DoFail(string.Format("DNS cannot resolve {0}", NodeConfig.Values["PUBLIC_HOSTNAME"]));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-accept-node command: {0} - {1}", ex.Message, ex.StackTrace);
                DoFail(ex.ToString());
            }
        }

        private void VerboseMessage(string message)
        {
            if (this.V)
            {
                Console.WriteLine(string.Format("Info: {0}", message));
            }
        }

        private void DoFail(string message)
        {
            Console.Error.WriteLine(string.Format("FAIL: {0}", message));
            this.status.ExitCode += 1;
        }

    }
}
