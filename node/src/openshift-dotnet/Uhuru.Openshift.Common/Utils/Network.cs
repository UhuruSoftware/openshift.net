using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Uhuru.Openshift.Common.Utils
{
    public class Network
    {
        public static void OpenFirewallPort(string port, string name)
        {
            Type netFwOpenPortType = Type.GetTypeFromProgID("HNetCfg.FWOpenPort");
            INetFwOpenPort openPort = (INetFwOpenPort)Activator.CreateInstance(netFwOpenPortType);
            openPort.Port = Convert.ToInt32(port);
            openPort.Name = name;
            openPort.Enabled = true;
            openPort.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;

            Type netFwMgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", false);
            INetFwMgr mgr = (INetFwMgr)Activator.CreateInstance(netFwMgrType);
            INetFwOpenPorts openPorts = (INetFwOpenPorts)mgr.LocalPolicy.CurrentProfile.GloballyOpenPorts;

            openPorts.Add(openPort);
        }

        public static void CloseFirewallPort(string port)
        {
            Type netFwMgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", false);
            INetFwMgr mgr = (INetFwMgr)Activator.CreateInstance(netFwMgrType);
            INetFwOpenPorts openPorts = (INetFwOpenPorts)mgr.LocalPolicy.CurrentProfile.GloballyOpenPorts;
            openPorts.Remove(Convert.ToInt32(port), NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);
        }

        private static List<int> GetPortRange(int ApplicationUid, int PortsPerUser, int MinUid, int StartPort = 10001)
        {
            List<int> ports = new List<int>();

            int startport = (ApplicationUid - MinUid) % ((65536 - StartPort) / PortsPerUser) * PortsPerUser + StartPort;

            for (int i = startport; i < startport + PortsPerUser; i++)
            {
                ports.Add(i);
            }
            return ports;
        }

        public static int GetAvailablePort(int ApplicationUid, string GearsDir, int PortsPerUser = 10, int MinUid = 1000, int StartPort = 10001)
        {
            List<int> availableport = GetPortRange(ApplicationUid, PortsPerUser, MinUid, StartPort);
            HashSet<int> occupiedports = GetOccupiedPorts(GearsDir);
            foreach (int port in availableport)
            {                
                    TcpClient tcpClient = new TcpClient();
                    try
                    {
                        tcpClient.Connect("127.0.0.1", port);
                    }
                    catch
                    {
                        if (!occupiedports.Contains(port))
                        {
                            return port;
                        }
                    }                
            }
            throw new Exception(string.Format("No available port for application Uid:{0}", ApplicationUid));
        }

        private static HashSet<int> GetOccupiedPorts(string GearsDir)
        {
            HashSet<int> Occupied = new HashSet<int>();  

            if (Directory.Exists(GearsDir))
            {
                foreach (string gear in Directory.GetDirectories(GearsDir))
                {
                    string GearEnv=Path.Combine(gear, ".env");
                    if (Directory.Exists(GearEnv))
                    {
                        if(File.Exists(Path.Combine(GearEnv,"PRISON_PORT")))
                        {
                            int OccupiedPort = 0;
                            string Port = File.ReadAllText(Path.Combine(GearEnv, "PRISON_PORT"));
                            if (Int32.TryParse(Port, out OccupiedPort))
                            {                               
                                    Occupied.Add(OccupiedPort);                               
                            }
                        }
                    }
                }
            }
            return Occupied;
        }

        public static int GetUniquePredictablePort(string portCounterFile)
        {
            // TODO: vladi: GLOBAL LOCK

            if (string.IsNullOrWhiteSpace(portCounterFile))
            {
                throw new ArgumentNullException("portCounterFile");
            }

            int counter = 0;

            if (File.Exists(portCounterFile))
            {
                string strCounter = File.ReadAllText(portCounterFile);
                Int32.TryParse(strCounter, out counter);
            }

            int port = 0;

            bool isAvailable = false;
            while (!isAvailable)
            {
                counter++;
                port = counter % 40000 + 10000;

                TcpClient tcpClient = new TcpClient();
                try
                {
                    tcpClient.Connect("127.0.0.1", port);
                }
                catch (Exception)
                {
                    isAvailable = true;
                }
            }

            File.WriteAllText(portCounterFile, counter.ToString());

            return port;

            // TODO: vladi: GLOBAL LOCK
        }
    }
}
