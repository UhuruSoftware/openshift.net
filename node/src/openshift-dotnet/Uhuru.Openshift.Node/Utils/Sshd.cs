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
    public class Sshd
    {
        public static void ConfigureSshd(string targetDir, string user, string windowsUser, string userHomeDir, string userShell)
        {
            if(string.IsNullOrEmpty(targetDir))
            {
                targetDir = @"C:\cygwin\installation";
            }
            if(string.IsNullOrEmpty(user))
            {
                user = "administrator";
            }
            if(string.IsNullOrEmpty(windowsUser))
            {
                windowsUser = "administrator";
            }
            if(string.IsNullOrEmpty(userHomeDir))
            {
                userHomeDir = @"C:\cygwin\administrator_home";
            }
            if(string.IsNullOrEmpty(userShell))
            {
                userShell = @"/bin/bash";
            }

            string passwdFile = Path.Combine(targetDir, "etc", "passwd");
            
            string userSID = null;
            try
            {
                var objUser = new NTAccount(windowsUser);
                userSID = ((SecurityIdentifier)objUser.Translate(typeof(SecurityIdentifier))).Value;
            }
            catch
            {
                throw new Exception(string.Format("Could not get SID for user {0}. Aborting.", windowsUser));
            }

            string usersGroupSID = CygwinPasswd.GetNoneGroupSID();
            Logger.Debug("Creating user home directory...");
            Directory.CreateDirectory(userHomeDir);

            string sshDir = Path.Combine(userHomeDir, ".ssh");
            Directory.CreateDirectory(sshDir);
            string authorizedKeys = Path.Combine(sshDir, "authorized_keys");
            if(!File.Exists(authorizedKeys))
            {
                Logger.Debug("Setting up empty authorized_keys file...");
                File.WriteAllText(authorizedKeys, "", Encoding.ASCII);
            }

            string keyFileLinux = LinuxFiles.Cygpath(authorizedKeys);
            LinuxFiles.Chmod(keyFileLinux, "600");

            Logger.Debug("Setting up user in passwd file...");
            string uid = userSID.Split('-').Last();
            string gid = usersGroupSID.Split('-').Last();
            string userHomeDirLinux = LinuxFiles.Cygpath(userHomeDir);
            userShell = LinuxFiles.Cygpath(userShell);
            File.AppendAllLines(passwdFile, 
                new List<string>() { string.Format("{0}:unused:{1}:{2}:{3},{4}:{5}:{6}", user, uid, gid, windowsUser, userSID, userHomeDirLinux, userShell) }, 
                Encoding.ASCII);

            Logger.Debug("Setting up user as owner of his home dir...");

            LinuxFiles.TakeOwnership(userHomeDir, windowsUser);
        }

        public static void RemoveUser(string targetDir, string user, string windowsUser, string userHomeDir, string userShell)
        {
            string passwdFile = Path.Combine(targetDir, "etc", "passwd");
            string userSID = null;
            try
            {
                var objUser = new NTAccount(windowsUser);
                userSID = ((SecurityIdentifier)objUser.Translate(typeof(SecurityIdentifier))).Value;
            }
            catch
            {
                throw new Exception(string.Format("Could not get SID for user {0}. Aborting.", windowsUser));
            }

            string usersGroupSID = CygwinPasswd.GetNoneGroupSID();

            Logger.Debug("Setting up user in passwd file...");
            string uid = userSID.Split('-').Last();
            string gid = usersGroupSID.Split('-').Last();
            string userHomeDirLinux = LinuxFiles.Cygpath(userHomeDir);
            userShell = LinuxFiles.Cygpath(userShell);
            string match = string.Format("{0}:unused:{1}:{2}:{3},{4}:{5}:{6}", user, uid, gid, windowsUser, userSID, userHomeDirLinux, userShell);
            List<string> content = File.ReadAllLines(passwdFile).ToList();
            content.Remove(match);
            File.WriteAllLines(passwdFile, content, Encoding.ASCII);
        }

        public static void AddKey(string targetDirectory, string user, string key)
        {
            RubyHash userInfo = CygwinPasswd.GetSSHDUser(targetDirectory, user);
            string homeDir = LinuxFiles.Cygpath(userInfo["home"], true);
            Directory.CreateDirectory(Path.Combine(homeDir, ".ssh"));
            string authorizedKeysFile = Path.Combine(homeDir, ".ssh", "authorized_keys");
            if(!File.Exists(authorizedKeysFile))
            {
                File.Create(authorizedKeysFile).Dispose();
            }
            Logger.Debug("Adding key to {0}", authorizedKeysFile);
            File.AppendAllLines(authorizedKeysFile, new string[] { key }, Encoding.ASCII);
        }

        public static void RemoveKey(string targetDirectory, string user, string key)
        {
            RubyHash userInfo = CygwinPasswd.GetSSHDUser(targetDirectory, user);
            string homeDir = LinuxFiles.Cygpath(userInfo["home"], true);
            string authorizedKeysFile = Path.Combine(homeDir, ".ssh", "authorized_keys");
            if(!File.Exists(authorizedKeysFile))
            {
                return;
            }
            List<string> content = File.ReadAllLines(authorizedKeysFile).ToList();
            content.Remove(key);
            File.WriteAllLines(authorizedKeysFile, content, Encoding.ASCII);            
        }
    }
}
