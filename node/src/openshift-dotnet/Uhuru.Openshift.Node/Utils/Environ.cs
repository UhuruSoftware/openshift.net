using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Uhuru.Openshift.Runtime.Config;

namespace Uhuru.Openshift.Runtime.Utils
{
    public class Environ
    {
        public static Dictionary<string, string> ForGear(string gearDir)
        {
            return ForGear(gearDir, new string[] { });
        }

        public static Dictionary<string, string> ForGear(string gearDir, params string[] dirs)
        {
            Dictionary<string, string> env = Load(Path.Combine(NodeConfig.ConfigDir, "env"));
            List<string> envDirs = new List<string>();
            envDirs.Add(Path.Combine(gearDir, ".env"));            
            foreach (string dir in Directory.GetDirectories(gearDir, "*"))
            {
                string envDir = Path.Combine(dir, "env");
                if (Directory.Exists(envDir))
                {
                    envDirs.Add(envDir);
                }
            }
            Load(envDirs.ToArray()).ToList().ForEach(x => env[x.Key] = x.Value);
            foreach (string dir in Directory.GetDirectories(Path.Combine(gearDir, ".env"), "*"))
            {
                if (dir.EndsWith("user_vars"))
                {
                    continue;
                }
                Load(dir).ToList().ForEach(x => env[x.Key] = x.Value);
            }
            foreach(string dir in dirs)
            {
                Load(Path.Combine(dir, "env")).ToList().ForEach(x => env[x.Key] = x.Value);
            }
            Load(Path.Combine(gearDir, ".env", "user_vars")).ToList().ForEach(x => env[x.Key] = x.Value);
            return env;
        }

        public static Dictionary<string, string> Load(params string[] dirs)
        {            
            Dictionary<string, string> env = new Dictionary<string, string>();
            foreach (string dir in dirs)
            {
                string[] files = Directory.GetFiles(dir);
                foreach (string file in files)
                {
                    if (file.EndsWith(".erb"))
                    {
                        continue;
                    }
                    env[Path.GetFileName(file)] = File.ReadAllText(file);
                }
            }
            return env;
        }
    }
}
