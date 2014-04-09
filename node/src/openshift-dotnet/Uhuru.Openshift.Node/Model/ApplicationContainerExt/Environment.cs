using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uhuru.Openshift.Common.JsonHelper;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Model.ApplicationContainerExt;
using Uhuru.Openshift.Runtime.Utils;
using Uhuru.Openshift.Utilities;


namespace Uhuru.Openshift.Runtime
{
    
    public partial class ApplicationContainer
    {

        const int USER_VARIABLE_MAX_COUNT = 25;
        const int USER_VARIABLE_NAME_MAX_SIZE  = 128;
        const int USER_VARIABLE_VALUE_MAX_SIZE = 512;
        string[] ALLOWED_OVERRIDES = new string[] {" "}; //TODO
        string[] RESERVED_VARIABLE_NAMES = new string[] { " " }; //TODO

        /// <summary>
        /// Retrieve user environment variable(s)
        /// </summary>
        public Dictionary<string, string> UserVarList(string[] variables)
        {  
            string userEnvDir = Path.Combine(ContainerDir, ".env", "user_vars");
            if (!Directory.Exists(userEnvDir))
            {
                return new Dictionary<string, string>();
            }
            Dictionary<string, string> env = Openshift.Runtime.Utils.Environ.Load(userEnvDir);
            if (variables == null || variables.Length == 0)
            {
                return env;
            }

            Dictionary<string, string> output = new Dictionary<string, string>();
            foreach (string variable in variables)
            {
                output.Add(variable, env[variable]);
            }

            return output;
        }


        /// <summary>
        /// Addes a environment variables.
        /// </summary>
        /// <param name="variables">The variables.</param>
        /// <param name="gears">The gears.</param>
        /// <returns></returns>
        public string AddUserVar(Dictionary<string, string> variables, List<string> gears)
        {
            string userEnvDir = Path.Combine(ContainerDir, ".env", "user_vars");
            if (!Directory.Exists(userEnvDir))
            {
                Directory.CreateDirectory(userEnvDir);
            }

            DirectoryInfo userEnvDirInfo = new DirectoryInfo(userEnvDir);
            if ((userEnvDirInfo.GetFiles().Length + variables.Count) > USER_VARIABLE_MAX_COUNT)
            {
                return string.Format("CLIENT_ERROR: User Variables maximum of {0} exceeded", USER_VARIABLE_MAX_COUNT);
            }
            foreach (KeyValuePair<string, string> variable in variables)
            {
                string path = Path.Combine(ContainerDir, ".env", variable.Key);
                if ((!ALLOWED_OVERRIDES.Contains(variable.Key) && File.Exists(path) ) ||
                    Regex.IsMatch(variable.Key,  @"\AOPENSHIFT_.*_IDENT\Z") ||
                    RESERVED_VARIABLE_NAMES.Contains(variable.Key))
                {
                    return string.Format("CLIENT_ERROR: name {0} cannot be overriden");
                }
                if (variable.Key.Length > USER_VARIABLE_NAME_MAX_SIZE)
                {
                    return string.Format("CLIENT_ERROR: name {0} exceeds maximum size of {1}b", variable.Key, USER_VARIABLE_NAME_MAX_SIZE);
                }

                if (variable.Value.Length > USER_VARIABLE_VALUE_MAX_SIZE)
                {
                    return string.Format("CLIENT_ERROR: {0} value exceeds maximum size of {1}b",variable.Key,  USER_VARIABLE_VALUE_MAX_SIZE);
                }

            }

            foreach (KeyValuePair<string, string> variable in variables)
            {
             
                string path = Path.Combine(userEnvDir, variable.Key);
                File.WriteAllText(path, variable.Value);
            }

            if (gears != null && gears.Count > 0)
            {
              return UserVarPush(gears, true);
            }

            return string.Empty;
        }

        /// <summary>
        /// Removes user variables.
        /// </summary>
        /// <param name="varNames">The variable names.</param>
        /// <param name="gears">The gears.</param>
        /// <returns></returns>
        public string UserVarRemove(List<string> varNames, List<string> gears)
        {
            string userEnvDir = Path.Combine(ContainerDir, ".env", "user_vars");
            foreach (string name in varNames)
            {
                string varPath = Path.Combine(userEnvDir, name);
                File.Delete(varPath);
            }

            if (gears != null && gears.Count > 0)
            {
                return UserVarPush(gears, true);
            }

            return string.Empty;
        }

        public string UserVarPush(List<string> gears, bool envAdd =false)
        {
            // TODO: implement this
            return string.Empty;
        }
        
        public string RemoveSshKey(string sshKey, string keyType, string comment)
        {
            string output = "";

            string key = string.Format("{0} {1} {2}", keyType, sshKey, comment);
            Sshd.RemoveKey(NodeConfig.Values["SSHD_BASE_DIR"], this.Uuid, key);
            return output;
        }

         public string RemoveSshKeys(List<SshKey> keys)
        {
            string output = "";
             foreach (SshKey key in keys)
             {
                  RemoveSshKey(key.Key, key.Type, key.Comment);
             }
             return output;
        }
        
        /// <summary>
        /// Add broker authorization keys so gear can communicate with broker.
        /// </summary>
        /// <param name="iv">A String value for the IV file.</param>
        /// <param name="token">A String value for the token file.</param>
        /// <returns>Returns An Array of Strings for the newly created auth files</returns>
        public string AddBrokerAuth(string iv, string token)
        {
            string brokerAuthDir = Path.Combine(ContainerDir, ".auth");
            StringBuilder output = new StringBuilder();

            Directory.CreateDirectory(brokerAuthDir);

            string ivFile = Path.Combine(brokerAuthDir, "iv");
            string tokenFile = Path.Combine(brokerAuthDir, "tolen");

            if (!File.Exists(ivFile))
            {
                output.AppendLine(ivFile);
            }
            if (!File.Exists(tokenFile))
            {
                output.AppendLine(tokenFile);
            }

            File.WriteAllText(ivFile, iv);
            File.WriteAllText(tokenFile, token);

            SetRWPermissions(brokerAuthDir);

            // TODO: Change permissions

            return output.ToString();
        }

        /// <summary>
        /// Removes the broker authentication keys from gear.
        /// </summary>
        /// <returns> Returns nil on Success and false on Failure</returns>
        public string RemoveBrokerAuth()
        {
            string brokerAuthDir = Path.Combine(ContainerDir, ".auth");
            try
            {
                Directory.Delete(brokerAuthDir, true);
            }
            catch (Exception ex)
            {
                //TODO: Logging
            }

            if (Directory.Exists(brokerAuthDir))
            {
                return "false";
            }
            else
            {
                return string.Empty;
            }
        }


        /// <summary>
        ///Remove an environment variable from a given gear.
        /// </summary>
        /// <param name="key">String name of the environment variable to remove.</param>
        /// <returns></returns>
        public string RemoveEnvVar(string key)
        {
            return RemoveEnvVar(key, null);
        }

        /// <summary>
        /// Remove an environment variable from a given gear.
        /// </summary>
        /// <param name="key">String name of the environment variable to remove.</param>
        /// 
        /// <returns>Returns string empty on success and false on failure.</returns>
        public string RemoveEnvVar(string key, string prefixCloudName)
        {
            string envDir = Path.Combine(ContainerDir, ".env");
            string userEnvDir = Path.Combine(ContainerDir, ".env", ".uservars");
            string status = string.Empty;

            if (!string.IsNullOrEmpty(prefixCloudName))
            {
                key = string.Format("OPENSHIFT_{0}", key);
            }

            string envFilePath = Path.Combine(envDir, key);

            try
            {
                File.Delete(envFilePath);
            }
            catch (Exception ex)
            {
                //TODO logging
                status = "false";
            }

            if (File.Exists(envFilePath))
            {
                status = "false";
            }

            return status;
        }
        
        public void ReplaceSshKeys(List<SshKey> sshKeys)
        {
            if (!ValidateSshKeys(sshKeys))
            {
                throw new Exception("The provided ssh keys do not have the required attributes");
            }

            List<SshKey> authorizedKeys = sshKeys.Where(item => item.Type != "krb5-principal").ToList<SshKey>();
            List<SshKey> krb5Principals = sshKeys.Where(item => item.Type == "krb5-principal").ToList<SshKey>();

            if (authorizedKeys.Count > 0)
            {
                new AuthorizedKeysFile(this).ReplaceKeys(sshKeys);
            }

            // TODO replace kerberos keys
        }

        public bool ValidateSshKeys(List<SshKey> sshKeys)
        {
            foreach (SshKey key in sshKeys)
            {
                try
                {
                    if (key.Comment == null || key.Key == null || key.Type == null)
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }
    }
}
