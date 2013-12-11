using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime
{
    
    public partial class ApplicationContainer
    {
        /// <summary>
        /// Retrieve user environment variable(s)
        /// </summary>
        public Dictionary<string, string> UserVarList(string[] variables)
        {  
            string userEnvDir = Path.Combine(ContainerDir, ".env", "user_vers");
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
    }
}
