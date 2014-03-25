using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Common.OODiagnostics.Tests
{
    public class TestLocalUsers :ITest
    {
        ExitCode exitcode = ExitCode.PASS;
        public string GetName()
        {
            return "test_local_users";
        }

        public void Run()
        {
            Output.WriteDebug("Testing local user consistency");
            List<string> localUsers = GetLocalUsers();
            Output.WriteDebug(string.Format("Found {0} local users", localUsers.Count));
            Prison.Prison[] prisonUsers = Prison.Prison.Load();
            List<string> usersNotInPrison = new List<string>();

            foreach (string localUser in localUsers)
            {
                if (!localUser.StartsWith("prison_"))
                {
                    //Skiping non prison users
                    continue;
                }
                Output.WriteDebug(string.Format("Testing local user {0}", localUser));
                if (prisonUsers.Where(p => p.User.Username == localUser).Count() ==0)
                {
                    usersNotInPrison.Add(localUser);
                }
            }
            
            if (usersNotInPrison.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("The following user(s)");
                sb.AppendLine(String.Join(", ", usersNotInPrison));
                sb.AppendLine("exists on the local system but there is no prison associated to them");
                Output.WriteWarn(sb.ToString());
                exitcode = ExitCode.WARNING;
            }
        }

        public ExitCode GetExitCode()
        {
           return exitcode;
        }

        private List<string> GetLocalUsers()
        {
            List<string> localUsers = new List<string>();
            SelectQuery query = new SelectQuery("Win32_UserAccount");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject envVar in searcher.Get())
            {
                localUsers.Add(envVar["Name"].ToString());
            }
            return localUsers;
        }
    }
}
