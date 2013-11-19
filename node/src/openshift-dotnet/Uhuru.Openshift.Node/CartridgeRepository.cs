using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Common.Models;
using YamlDotNet.RepresentationModel.Serialization;

namespace Uhuru.Openshift.Runtime
{
    public class CartridgeRepository
    {
        private static CartridgeRepository instance;
        private CartridgeRepository() 
        {
            LatestVersions = new List<Cartridge>();
            foreach (string dir in Directory.GetDirectories(CartridgeRepository.RepositoryPath))
            {
                string manifestPath = Path.Combine(dir, "metadata", "manifest.yml");
                if (File.Exists(manifestPath))
                {
                    string document = File.ReadAllText(manifestPath);
                    var input = new StringReader(document);
                    var deserializer = new Deserializer();
                    dynamic spec = (dynamic)deserializer.Deserialize(input);
                    LatestVersions.Add(Cartridge.FromDescriptor(spec));
                }
            }
        }

        public List<Cartridge> LatestVersions
        {
            get;
            set;
        }

        public static CartridgeRepository Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CartridgeRepository();
                }
                return instance;
            }
        }

        public static string RepositoryPath
        {
            get
            {
                string binLocation = Path.GetDirectoryName(typeof(CartridgeRepository).Assembly.Location);
                return Path.GetFullPath(Path.Combine(binLocation, @"..\..\cartridges"));
            }
        }
    }
}
