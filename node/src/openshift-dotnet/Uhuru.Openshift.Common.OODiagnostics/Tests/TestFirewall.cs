using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Uhuru.Openshift.Common.OODiagnostics.Tests
{
    public class TestFirewall :ITest
    {
        const string SSHDFWRULENAME = "Openshift SSHD Port";
        
        ExitCode exitCode = ExitCode.PASS;

        public string GetName()
        {
            return "test_firewall";
        }

        public void Run()
        {
            Output.WriteDebug("Testing if firewall is enables ");
            Type netFwMgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", false);
            INetFwMgr mgr = (INetFwMgr)Activator.CreateInstance(netFwMgrType);
            bool firewallEnabled = mgr.LocalPolicy.CurrentProfile.FirewallEnabled;

            if (!firewallEnabled)
            {
                Output.WriteWarn("The windows firewall is disabled on the local machine");
                exitCode = ExitCode.WARNING;
                return;
            }

            Output.WriteDebug("Testing if ssh port is opened");
            CheckRule(SSHDFWRULENAME, 22);

            Output.WriteDebug("Testing prison firewall rulles");
            Prison.Prison[] prisonUsers = Prison.Prison.Load();
            foreach (var prisonUser in prisonUsers)
            {
                string firewallRuleName = prisonUser.ID.ToString().TrimStart('0').Replace("-", "");
                Output.WriteDebug(string.Format("Testing firewall for user {0}", firewallRuleName));
                int firewallPort = prisonUser.Rules.UrlPortAccess;
                CheckRule(firewallRuleName, firewallPort);
            }
            
        }

        private void CheckRule(string ruleName, int port)
        {
            string output = string.Empty;
            Type netFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2", false);
            INetFwPolicy2 fwpol = (INetFwPolicy2)Activator.CreateInstance(netFwPolicy2);
            try
            {
                INetFwRule rule = fwpol.Rules.Item(ruleName);
                string localPort =  rule.LocalPorts;
                if (localPort != port.ToString())
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(string.Format("Incorect Local Port setup for rule {0}", ruleName));
                    sb.AppendLine(string.Format("Expected {0} but is {1}", port, localPort));
                    output = sb.ToString();                    
                }

            }
            catch (FileNotFoundException)
            {
                output = string.Format("Rule {0} does not exist", ruleName);
            }

            if (!string.IsNullOrEmpty(output))
            {
                Output.WriteWarn(output);
                exitCode = ExitCode.WARNING;
            }

        }

        public ExitCode GetExitCode()
        {
            return exitCode;
        }
    }
}
