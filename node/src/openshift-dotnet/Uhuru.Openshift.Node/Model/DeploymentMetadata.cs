using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime
{
    public class DeploymentMetadata
    {
        public string GitRef
        {
            get
            {
                return this.metadata["git_ref"] as string;
            }
            set
            {
                this.metadata["git_ref"] = value;
            }
        }

        public string GitSha
        {
            get
            {
                return this.metadata["git_sha"] as string;
            }
            set
            {
                this.metadata["git_sha"] = value;
            }
        }

        public string Id
        {
            get
            {
                return this.metadata["id"] as string;
            }
            set
            {
                this.metadata["id"] = value;
            }
        }

        public bool HotDeploy
        {
            get
            {
                bool result = false;
                bool.TryParse(this.metadata["hot_deploy"] as string, out result);
                return result;                
            }
            set
            {
                this.metadata["hot_deploy"] = value.ToString();
            }
        }

        public bool ForceCleanBuild
        {
            get
            {
                bool result = false;
                bool.TryParse(this.metadata["force_clean_build"] as string, out result);
                return result;
            }
            set
            {
                this.metadata["force_clean_build"] = value.ToString();
            }
        }

        public List<float> Activations
        {
            get
            {
                if (this.metadata["activations"] is List<float>)
                {
                    return (List<float>)this.metadata["activations"];
                }
                else
                {
                    return ((JArray)this.metadata["activations"]).Select(t => t.ToObject<float>()).ToList();
                }
            }
            set
            {
                this.metadata["activations"] = value;
            }
        }

        public string Checksum
        {
            get
            {
                return this.metadata["checksum"] as string;
            }
            set
            {
                this.metadata["checksum"] = value;
            }
        }

        string file;
        Dictionary<string, object> metadata;

        Dictionary<string, object> defaults = new Dictionary<string, object>()
        {
            {"git_ref", "master"},
            {"git_sha", null},
            {"id", null},
            {"hot_deploy", null},
            {"force_clean_build", null},
            {"activations", new List<float> {}},
            {"checksum", null}
        };

        public DeploymentMetadata(ApplicationContainer container, string deploymentDatetime)
        {
            this.file = Path.Combine(container.ContainerDir, "app-deployments", deploymentDatetime, "metadata.json");
            if(File.Exists(file))
            {
                Load();
            }
            else
            {
                using (File.Create(this.file)) { };
                container.SetRWPermissions(this.file);
                this.metadata = this.defaults;
                Save();
            }
        }

        public void RecordActivation()
        {
            this.Activations.Add(DateTime.Now.Ticks);
        }

        public void Load()
        {
            this.metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(file));
        }

        public void Save()
        {
            File.WriteAllText(this.file, JsonConvert.SerializeObject(this.metadata));
        }
    }
}
