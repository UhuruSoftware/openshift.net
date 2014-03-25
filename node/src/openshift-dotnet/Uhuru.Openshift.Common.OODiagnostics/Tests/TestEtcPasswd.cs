using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Utils;

namespace Uhuru.Openshift.Common.OODiagnostics.Tests
{
    public class TestEtcPasswd : ITest
    {

        const string ETCPATH = @"C:\openshift\cygwin\installation\etc\passwd";

        ExitCode exitCode = ExitCode.PASS;
        public string GetName()
        {
            return "test_etc_passwd";
        }

        public void Run()
        {
            List<string> usersNotInPrison = new List<string>();
            List<string> usersWithNoGears = new List<string>();

            Output.WriteDebug("Checking cygwing passwd file consistency");
            NodeConfig nodeConfig = new NodeConfig();
            Etc etc = new Etc(nodeConfig);
            EtcUser[] etcUsers = etc.GetAllUsers();
            Output.WriteDebug(string.Format("Found {0} Etc Users", etcUsers));

            Prison.Prison[] prisons = Prison.Prison.Load();
            Output.WriteDebug(string.Format("Found {0} Prison Users", prisons.Count()));

            List<ApplicationContainer> gears =  ApplicationContainer.All(null, false).ToList<ApplicationContainer>();
            Output.WriteDebug(string.Format("Found {0} gears", gears.Count()));

            foreach (EtcUser etcUser in etcUsers)
            {
                Output.WriteDebug(string.Format("Checking user {0}", etcUser.Name));

                if (etcUser.Name == "administrator")
                {
                    //skipping administrator user
                    continue;
                }

               if (prisons.Where(p => p.ID.ToString().TrimStart('0').Replace("-", "") == etcUser.Name).Count() == 0)
               {
                   usersNotInPrison.Add(etcUser.Name);
               }
               
               if (gears.Where(g => g.Uuid == etcUser.Name).Count() == 0)
               {
                   usersWithNoGears.Add(etcUser.Name);
               }
            }

            if (usersNotInPrison.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("The following users exist in /etc/passwd");
                sb.AppendLine(String.Join(", ", usersNotInPrison.ToArray()));
                sb.AppendLine("but have no prison user associated to them");
                Output.WriteWarn(sb.ToString());
                exitCode = ExitCode.WARNING;
            }
            
            if (usersWithNoGears.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("The following users exist in /etc/passwd");
                sb.AppendLine(String.Join(", ", usersWithNoGears.ToArray()));
                sb.AppendLine("but have no gears associated to them");
                Output.WriteWarn(sb.ToString());
                exitCode = ExitCode.WARNING;
            }

        }

        public ExitCode GetExitCode()
        {
            return exitCode;
        }
    }
}
