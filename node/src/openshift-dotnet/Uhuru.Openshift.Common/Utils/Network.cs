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

        public static int GetAvailablePort(int ApplicationUid,int PortsPerUser=10,int MinUid=1000, int StartPort =10001)
        {
            List<int> availableport = GetPortRange(ApplicationUid, PortsPerUser, MinUid, StartPort);
            List<int> occupiedports = GetOccupiedPorts();
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

        private static List<int> GetOccupiedPorts()
        {
            List<int> occupied = new List<int>();

            string BaseDir = Environment.GetEnvironmentVariable("OPENSHIFT_CONF_DIR") ?? @"c:\openshift\";
            string GearsDir = Path.Combine(BaseDir, "gears");

            if (Directory.Exists(GearsDir))
            {
                foreach (string gear in Directory.GetDirectories(GearsDir))
                {
                    string GearEnv=Path.Combine(gear, ".env");
                    if (Directory.Exists(GearEnv))
                    {
                        if(File.Exists(Path.Combine(GearEnv,"PRISON_PORT")))
                        {
                            int occupiedport = 0;
                            string port = File.ReadAllText(Path.Combine(GearEnv, "PRISON_PORT"));
                            Int32.TryParse(port, out occupiedport);
                            if (occupiedport > 0)
                            {
                                occupied.Add(occupiedport);
                            }
                        }
                    }
                }
            }
            return occupied;
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
