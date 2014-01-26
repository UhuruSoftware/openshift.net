using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Uhuru.Openshift.Common.Utils
{
    public class Network
    {
        /// <summary>
        /// This method returns a free port.
        /// </summary>
        /// <returns>An int that is the free port.</returns>
        public static int GrabEphemeralPort()
        {
            TcpListener socket = new TcpListener(IPAddress.Any, 0);
            socket.Start();
            int port = ((IPEndPoint)socket.LocalEndpoint).Port;
            socket.Stop();
            return port;
        }

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
    }
}
