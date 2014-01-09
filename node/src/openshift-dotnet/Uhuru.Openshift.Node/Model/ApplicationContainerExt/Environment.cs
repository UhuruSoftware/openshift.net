using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
            throw new NotImplementedException();
        }
    }
}
