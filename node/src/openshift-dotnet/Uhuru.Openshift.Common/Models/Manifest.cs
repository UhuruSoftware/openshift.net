using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Uhuru.Openshift.Common.Models;
using YamlDotNet.RepresentationModel.Serialization;

namespace Uhuru.Openshift.Runtime
{
    public class Manifest
    {
        public string PrivateIpName { get; set; }
        public string PrivatePortName { get; set; }
        public int PrivatePort { get; set; }
        public string PublicPortName { get; set; }
        public string WebsocketPortName { get; set; }
        public int WebsocketPort { get; set; }
        public string RepositoryPath { get; set; }
        public string Dir { get { return this.Name.ToLower(); } }
        public string Name { get; set; }
        public string CartridgeVersion { get; set; }
        public string ShortName { get; set; }

        public string OriginalName { get; set; }
        public string Version { get; set; }
        public HashSet<string> Versions { get; set; }
        public string Architecture { get; set; }
        public string DisplayName { get; set; }
        public string License { get; set; }
        public string LicenseUrl { get; set; }
        public string Vendor { get; set; }
        public string CartridgeVendor { get; set; }
        public string Description { get; set; }
        public List<object> Provides { get; set; }
        public List<object> Requires { get; set; }
        public List<object> Conflicts { get; set; }
        public List<object> NativeRequires { get; set; }
        public List<object> Categories { get; set; }
        public string Website { get; set; }
        public string SourceUrl { get; set; }
        public string SourceMD5 { get; set; }
        public string ManifestPath { get; set; }

        public List<object> Suggests { get; set; }
        bool isDeployable;
        public dynamic HelpTopics { get; set; }

        public dynamic CartDataDef { get; set; }

        public List<object> AdditionalControlActions { get; set; }


        public string DefaultProfile { get; set; }

        public bool Deployable
        {
            get
            {
                return this.isDeployable;
            }
        }

        public bool WebFramework
        {
            get
            {
                return Deployable;
            }
        }

        public bool Buildable
        {
            get
            {
                return Deployable;
            }
        }

        public bool WebProxy
        {
            get
            {
                return this.isWebProxy;
            }
        }

        public bool InstallBuildRequired
        {
            get
            {
                if (ManifestSpec.ContainsKey("Install-Build-Required"))
                {
                    return bool.Parse(ManifestSpec["Install-Build-Required"]);
                }
                return false;
            }
        }

        public dynamic ManifestSpec { get; set; }
        bool isWebProxy;

        public List<Uhuru.Openshift.Common.Models.Endpoint> Endpoints { get; set; }

        public Manifest(dynamic desc, string version = null, string type = "url", string repositoryBasePath = "", bool checkNames = true)
        {
            if(type == "url")
            {
                if (desc is string)
                {
                    this.ManifestSpec = ManifestFromYaml(desc);
                }
                else
                {
                    this.ManifestSpec = desc;
                }
                this.ManifestPath = "url";
                
            }
            else
            {
                string yml = File.ReadAllText(desc);
                this.ManifestSpec = ManifestFromYaml(yml);
                this.ManifestPath = desc;
            }

            this.Versions = new HashSet<string>();
            if(this.ManifestSpec.ContainsKey("Versions"))
            {
                foreach(var ver in this.ManifestSpec["Versions"])
                {
                    Versions.Add(ver.ToString());
                }
            }

            this.Versions.Add(this.ManifestSpec["Version"].ToString());
            this.CartridgeVersion = this.ManifestSpec["Cartridge-Version"].ToString();
            if (!string.IsNullOrEmpty(version))
            {
                this.Version = version;
                this.ManifestSpec["Version"] = this.Version;
            }
            else
            {
                this.Version = ManifestSpec["Version"];
            }

            if (this.ManifestSpec.ContainsKey("Version-Overrides"))
            {
                if (this.ManifestSpec["Version-Overrides"].ContainsKey(this.Version))
                {
                    Dictionary<object, object> vtree = this.ManifestSpec["Version-Overrides"][this.Version];
                    foreach (string key in vtree.Keys)
                    {
                        this.ManifestSpec[key] = vtree[key];
                    }
                }
            }

            this.CartridgeVendor = this.ManifestSpec["Cartridge-Vendor"];
            this.Name = ManifestSpec["Name"];
            this.ShortName = ManifestSpec["Cartridge-Short-Name"];
            this.Categories = ManifestSpec["Categories"] ?? new List<object>() { };
            this.isDeployable = this.Categories.Contains("web_framework");
            this.isWebProxy = this.Categories.Contains("web_proxy");

            string repositoryDirectory = string.Format("{0}-{1}", CartridgeVendor.ToLower(), this.Name);
            this.RepositoryPath = Path.Combine(repositoryBasePath, repositoryDirectory, CartridgeVersion);

            if (((Dictionary<object, object>)ManifestSpec).ContainsKey("Source-Url"))
            {
                if (!Uri.IsWellFormedUriString(ManifestSpec["Source-Url"], UriKind.Absolute))
                {
                    throw new Exception("Source-Url is not valid");                    
                }
                SourceUrl = ManifestSpec["Source-Url"];
                SourceMD5 = ManifestSpec.ContainsKey("Source-Md5") ? ManifestSpec["Source-Md5"] : null;
            }
            else
            {
                if (ManifestPath == "url")
                {
                    throw new MissingFieldException("Source-Url is required in manifest to obtain cartridge via URL", "Source-Url");
                }
            }


            this.Endpoints = new List<Endpoint>();
            if (((Dictionary<object, object>)ManifestSpec).ContainsKey("Endpoints"))
            {
                if (ManifestSpec["Endpoints"] is List<object>)
                {
                    foreach (dynamic ep in ManifestSpec["Endpoints"])
                    {
                        this.Endpoints.Add(Endpoint.FromDescriptor(ep, this.ShortName.ToUpper()));
                    }
                }
            }

            this.isWebProxy = Categories.Contains("web_framework");
        }



        public static string BuildIdent(string vendor, string software, string softwareVersion, string cartridgeVersion)
        {
            vendor = vendor.ToLower();
            return string.Format("{0}:{1}:{2}:{3}", vendor, software, softwareVersion, cartridgeVersion);
        }

        public static string[] ParseIdent(string ident)
        {
            string[] cooked = ident.Split(':');
            if (cooked.Length != 4)
            {
                throw new ArgumentException(string.Format("'{0}' is not a legal cartridge identifier", ident));
            }
            return cooked;
        }

        public static dynamic ManifestFromYaml(string yamlStr)
        {
            var input = new StringReader(yamlStr);
            var deserializer = new Deserializer();
            dynamic spec = (dynamic)deserializer.Deserialize(input);
            return spec;
        }

        public static List<string> SortVersions(IEnumerable<string> array)
        {
            List<string> copy = new List<string>(array);
            copy.RemoveAll(v => v == "_");

            copy.Sort(delegate(string x, string y){
                return CompareVersions(x, y);
            });

            return copy;
        }

        public static int CompareVersions(string v1, string v2)
        {
            List<int> versions1 = new List<int>();
            List<int> versions2 = new List<int>();

            foreach(string v in v1.Split('.'))
            {
                versions1.Add(int.Parse(v));
            }

            foreach (string v in v2.Split('.'))
            {
                versions2.Add(int.Parse(v));
            }

            while (versions1.Count < versions2.Count) { versions1.Add(0); }
            while (versions2.Count < versions1.Count) { versions2.Add(0); }

            for (int i = 0; i < versions1.Count; i++)
            {
                if (versions1[i] > versions2[i])
                    return 1;
                else if (versions1[i] < versions2[i])
                    return -1;
            }

            return 0;
        }

        public Manifest ProjectVersionOverrides(string version, string repositoryBasePath)
        {
            return new Manifest(this.ManifestPath, version, "file", repositoryBasePath);
        }

        public string ToManifestString()
        {
            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb))
            {
                var serializer = new Serializer();
                serializer.Serialize(sw, this.ManifestSpec);
            }
            return sb.ToString();
        }

        public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<Cartridge: ");
            foreach(System.ComponentModel.PropertyDescriptor property in TypeDescriptor.GetProperties(this))
            {
                sb.Append(string.Format("{0}: {1} ", property.Name, property.GetValue(this)));
            }
            sb.Append(" >");
            return sb.ToString();
        }
    }
}
