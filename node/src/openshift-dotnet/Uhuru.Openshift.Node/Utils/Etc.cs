using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Uhuru.Openshift.Runtime.Config;

namespace Uhuru.Openshift.Runtime.Utils
{
    public class Etc
    {
        string passwdPath;
        NodeConfig config;

        public Etc(NodeConfig config)
        {
            passwdPath = Path.Combine(config.Get("SSHD_BASE_DIR"), "etc", "passwd");
            this.config = config;
        }

        public EtcUser[] GetAllUsers()
        {
            List<EtcUser> result = new List<EtcUser>();

            string[] passwdFile = File.ReadAllLines(passwdPath);

            foreach (string line in passwdFile)
            {
                string[] passwdProperties = line.Split(':');
                if (passwdProperties.Length > 1)
                {
                    EtcUser etcUser = new EtcUser()
                    {
                        Name = passwdProperties[0],
                        Passwd = passwdProperties[1],
                        Uid = int.Parse(passwdProperties[2]),
                        Gid = int.Parse(passwdProperties[3]),
                        Gecos = passwdProperties[4],
                        Dir = passwdProperties[5],
                        Shell = passwdProperties[6],
                    };

                    result.Add(etcUser);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Gets the user information with the specified login name.
        /// </summary>
        /// <param name="name">The name of the user.</param>
        /// <returns>EtcUser</returns>
        public EtcUser GetPwanam(string name)
        {
            EtcUser etcUser = new EtcUser();
            string[] passwdFile = File.ReadAllLines(passwdPath);

            int uid = 0;
            int gid = 0;
            string passwd = string.Empty;
            string gecos = string.Empty;
            string dir = string.Empty;
            string shell = string.Empty;

            foreach (string line in passwdFile)
            {
                string[] passwdProperties = line.Split(':');
                if (passwdProperties.Length > 1)
                {
                    if (passwdProperties[0] == name)
                    {
                        uid = int.Parse(passwdProperties[2]);
                        gid = int.Parse(passwdProperties[3]);
                        passwd = passwdProperties[1];
                        gecos = passwdProperties[4];
                        dir = passwdProperties[5];
                        shell = passwdProperties[6];
                    }
                }
            }

            if (uid == 0)
            {
                throw new Exception(string.Format("User {0} does not exist", name));
            }

            etcUser.Name = name;
            etcUser.Uid = uid;
            etcUser.Gid = gid;
            etcUser.Passwd = passwd;
            etcUser.Gecos = gecos;
            etcUser.Dir = dir;
            etcUser.Shell = shell;

            return etcUser;
        }
        
    }
}
