using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Common
{
    public class Config
    {
        private static Dictionary<string, ParseConfig> confParsed;
        private static Dictionary<string, DateTime> confMtime;

        private ParseConfig config;

        /// <summary>
        /// Initializes a new instance of the <see cref="Config"/> class.
        /// </summary>
        /// <param name="configPath">The configuration path.</param>
        /// <param name="defaults">The defaults.</param>
        public Config(string configPath)
        {
            if (!string.IsNullOrEmpty(configPath))
            {
                if (confParsed == null)
                {
                    confParsed = new Dictionary<string, ParseConfig>();
                }
                if (confMtime == null)
                {
                    confMtime = new Dictionary<string, DateTime>();
                }
                try
                {
                    DateTime configWriteTime = (new FileInfo(configPath)).LastWriteTime;
                    if ((!confParsed.ContainsKey(configPath)) || (configWriteTime != confMtime[configPath]))
                    {
                        
                        confParsed[configPath] = new ParseConfig(configPath);
                        confMtime[configPath] = configWriteTime;
                        
                    }
                    config = confParsed[configPath];
                }
                catch (IOException ioException)
                {
                    throw ioException;
                }
            }
        }

        /// <summary>
        /// Gets the value from specified config name.
        /// </summary>
        /// <param name="name">The config name.</param>
        /// <returns></returns>
        public string Get(string name)
        {
            return Get(name, "");
        }

        /// <summary>
        /// Gets the value from specified config name.
        /// </summary>
        /// <param name="name">The config name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public string Get(string name, string defaultValue)
        {
            return config.GetValue(name, "", defaultValue);
        }

        /// <summary>
        /// Gets a boolean value.
        /// </summary>
        /// <param name="name">The config name.</param>
        /// <param name="def">The default value.</param>
        /// <returns></returns>
        public bool GetBool(string name, bool defaultValue)
        {
            string value = Get(name, defaultValue.ToString()).ToLower();
            if (value == "true" || value == "yes" || value == "t" || value == "y" || value == "1")
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a group.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="defaults">The defaults.</param>
        /// <returns></returns>
        public Dictionary<string, string> GetGroup(string sectionName, Dictionary<string, string> defaults)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            if (defaults == null)
            {
                defaults = new Dictionary<string, string>();
            }
            string[] sectionKeys = config.GetKeys(sectionName);
            foreach (string key in sectionKeys)
            {
                string value = "";
                if (defaults.ContainsKey(key))
                {
                    value = config.GetValue(key, sectionName, defaults[key]);
                }
                else
                {
                    value = config.GetValue(key, sectionName);
                }
                values.Add(key, value);
            }
            return values;
        }

        /// <summary>
        /// Gets all the config keys.
        /// </summary>
        /// <returns>List containing all the configuration keys</returns>
        public string[] GetParams()
        {
            return config.GetKeys(""); 
        }


        /// <summary>
        /// Gets the <see cref="System.String"/> with the specified key.
        /// Same as calling Config.Get(key).
        /// </summary>
        /// <value>
        /// The <see cref="System.String"/> value associated with the key in the config file.
        /// </value>
        /// <param name="key">The key for which to get the value.</param>
        /// <returns>The value associated with the key in the configuration file.</returns>
        public string this[string key]
        {
            get 
            {
                return this.Get(key);
            }
        }
    }
}
