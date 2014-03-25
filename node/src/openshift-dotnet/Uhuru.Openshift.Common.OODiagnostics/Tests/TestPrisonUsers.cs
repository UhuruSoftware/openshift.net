using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;

namespace Uhuru.Openshift.Common.OODiagnostics.Tests
{
    public class TestPrisonUsers :ITest
    {
        ExitCode exitCode = ExitCode.PASS;
        public string GetName()
        {
            return "test_prison_users";
        }

        public void Run()
        {
            Output.WriteDebug("Checking Uhuru Prison users existance");
            Prison.Prison[] prisonUsers = Prison.Prison.Load();
            List<string> notExistingUsers = new List<string>();
            Output.WriteDebug(string.Format("Found {0} prison users on the system", prisonUsers.Count()));

            foreach (var prisonUser in prisonUsers)
            {
                Output.WriteDebug(string.Format("Testing user {0}", prisonUser.User.Username));
                using (PrincipalContext principalContext = new PrincipalContext(ContextType.Machine))
                {
                    UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(principalContext,
                        IdentityType.SamAccountName,
                        prisonUser.User.Username);
                    if (userPrincipal  == null)
                    {
                        notExistingUsers.Add(prisonUser.User.Username);
                        Output.WriteDebug(string.Format("User {0} does not exist on the system", prisonUser.User.Username));
                    }
                }
            }

            if (notExistingUsers.Count > 0)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("The following prison users do not exist on the local system");
                stringBuilder.AppendLine(String.Join(", ", notExistingUsers));
                Output.WriteWarn(stringBuilder.ToString());
                exitCode = ExitCode.WARNING;
            }

        }

        public ExitCode GetExitCode()
        {
            return exitCode;
        }
    }
}
