using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Runtime.Utils
{
    class CygwinPasswd
    {
        public static string GetNoneGroupSID()
        {
            try
            {
                var objUsersGroup = new NTAccount("None");
                return ((SecurityIdentifier)objUsersGroup.Translate(typeof(SecurityIdentifier))).Value;
            }
            catch
            {
                throw new Exception("Could not get SID for the local 'None' group. Aborting.");
            }
        }

        public static RubyHash GetSSHDUsers(string cygwinPath)
        {
            string passwdFile = Path.Combine(cygwinPath, "etc", "passwd");

            RubyHash users = new RubyHash();

            foreach (string line in File.ReadAllLines(passwdFile))
            {
                string[] userInfo = line.Split(':');
                if (userInfo.Length == 7)
                {                    
                    string user = userInfo[0];
                    string windowsUserSID = userInfo[4].Split(',')[1];
                    users[user] = new RubyHash() {
                        {"user", userInfo[0]},
                        {"uid", userInfo[2]},
                        {"gid", userInfo[3]},
                        {"sid", windowsUserSID},
                        {"home", userInfo[5]},
                        {"shell", userInfo[6]}
                    };
                }
            }
            return users;
        }

        public static RubyHash GetSSHDUser(string cygwinPath, string user)
        {
            return GetSSHDUsers(cygwinPath)[user];
        }
    }
}
