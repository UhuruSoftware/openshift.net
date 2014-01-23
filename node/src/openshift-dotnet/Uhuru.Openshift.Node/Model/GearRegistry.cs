using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Uhuru.Openshift.Runtime.Model
{
    public class GearRegistry
    {
        public class Entry
        {
            public string Uuid { get; set; }
            public string Namespace { get; set; }
            public string Dns { get; set; }
            public string ProxyHostname { get; set; }
            public string ProxyPort { get; set; }

            public Entry(dynamic options)
            {
                this.Uuid = options["uuid"];
                this.Namespace = options["namespace"];
                this.Dns = options["dns"];
                this.ProxyHostname = options["proxy_hostname"];
                this.ProxyPort = options["proxy_port"];
            }

            public string ToJson()
            {
                return JsonConvert.SerializeObject(new Dictionary<string, string>()
                {
                    {"namespace", this.Namespace},
                    {"dns", this.Dns},
                    {"proxy_hostname", this.ProxyHostname},
                    {"proxy_port", this.ProxyPort}
                });
            }

            public string ToSshUrl()
            {
                return string.Format("{0}@{1}", this.Uuid, this.ProxyHostname);
            }
        }

        public Dictionary<string, object> Entries
        {
            get
            {
                return this.gearRegistry.ToDictionary(entry => entry.Key, entry => entry.Value);
            }
        }
        
        ApplicationContainer container;
        string registryFile;
        string backupFile;
        string lockFile;
        Dictionary<string, object> gearRegistry;


        public GearRegistry(ApplicationContainer container)
        {
            this.container = container;
            string baseDir = Path.Combine(this.container.ContainerDir, "gear-registry");
            Directory.CreateDirectory(baseDir);
            this.registryFile = Path.Combine(baseDir, "gear-registry.json");
            if(!File.Exists(this.registryFile))
                File.Create(this.registryFile);
            this.container.SetRoPermissions(this.registryFile);
            this.backupFile = Path.Combine(baseDir, "gear-registry.json.bak");
            this.lockFile = Path.Combine(baseDir, "gear-registry.lock");
            Load();
        }

        public void Load()
        {
            Clear();
            WithLock(delegate()
            {
                string json = File.ReadAllText(this.registryFile);
                Dictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                foreach (KeyValuePair<string, object> pair in values)
                {
                    Dictionary<string, object> entries = (Dictionary<string, object>)pair.Value;
                    foreach (KeyValuePair<string, object> entry in entries)
                    {
                        Dictionary<string, object> options = (Dictionary<string, object>)entry.Value;
                        options["type"] = pair.Key;
                        options["uuid"] = entry.Key;
                        Add(options);
                    }
                }
            });
        }

        public void Clear()
        {
            this.gearRegistry = new Dictionary<string, object>();
        }

        public void Add(Dictionary<string, object> options)
        {
            foreach(string s in new string[] {"type", "uuid", "namespace", "dns", "proxy_hostname", "proxy_port"})
            {
                if (!options.ContainsKey(s))
                {
                    throw new ArgumentNullException(s);
                }
            }

            string type = options["type"].ToString();
            if (this.gearRegistry[type] == null)
            {
                this.gearRegistry[type] = new Dictionary<string, object>();
            }
            ((Dictionary<string, object>)this.gearRegistry[type])[options["uuis"].ToString()] = new Entry(options);
        }

        public delegate void WithLockCallback();
        public void WithLock(WithLockCallback action)
        {
            Mutex mutex = new Mutex(false, this.lockFile.Replace("\\", ""));
            try
            {
                mutex.WaitOne();
                action();
            }
            catch
            {

            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }
}
