using System;
using System.Collections.Generic;
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
        public List<object> Versions { get; set; }
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
        public List<object> Suggests { get; set; }

        public dynamic HelpTopics { get; set; }

        public dynamic CartDataDef { get; set; }

        public List<object> AdditionalControlActions { get; set; }

        public List<Uhuru.Openshift.Common.Models.Profile> Profiles { get; set; }

        public string DefaultProfile { get; set; }

        public Dictionary<string, Uhuru.Openshift.Common.Models.Profile> ProfileMap { get; set; }

        public bool WebProxy
        {
            get
            {
                return this.isWebProxy;
            }
        }

        dynamic manifest;
        bool isWebProxy;

        public List<Uhuru.Openshift.Common.Models.Endpoint> Endpoints { get; set; }

        public Manifest(dynamic desc, string version, string type, string repositoryBasePath, bool checkNames)
        {
            if (desc is String)
            {
                this.manifest = ManifestFromYaml(desc);
            }
            else
            {
                this.manifest = desc;
            }
            this.Versions = this.manifest.ContainsKey("Versions") ? this.manifest["Versions"] : new List<object>();
            this.Versions.Add(this.manifest["Version"]);
            this.CartridgeVersion = this.manifest["Cartridge-Version"].ToString();
            if (!string.IsNullOrEmpty(version))
            {
                this.Version = version;
                this.manifest["Version"] = this.Version;
            }
            else
            {
                this.Version = manifest["Version"];
            }

            this.CartridgeVendor = this.manifest["Cartridge-Vendor"];
            this.Name = manifest["Name"];
            this.ShortName = manifest["Cartridge-Short-Name"];
            this.Categories = manifest["Categories"] ?? new List<object>() { };

            this.RepositoryPath = Path.Combine(repositoryBasePath, this.Name);

            this.Endpoints = new List<Endpoint>();
            if (((Dictionary<object, object>)manifest).ContainsKey("Endpoints"))
            {
                if (manifest["Endpoints"] is List<object>)
                {
                    foreach (dynamic ep in manifest["Endpoints"])
                    {
                        this.Endpoints.Add(Endpoint.FromDescriptor(ep));
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

        public static dynamic ManifestFromYaml(string yamlStr)
        {
            var input = new StringReader(yamlStr);
            var deserializer = new Deserializer();
            dynamic spec = (dynamic)deserializer.Deserialize(input);
            return spec;
        }


    }
}
