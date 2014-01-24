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
            etcUser.Gecons = config.Get("GEAR_GECOS");
            etcUser.Dir = Path.Combine(config.Get("GEAR_BASE_DIR"), name);
            etcUser.Shell = config.Get("GEAR_SHELL");

            return etcUser;
        }
        
    }
}
