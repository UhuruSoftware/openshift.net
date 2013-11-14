using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Uhuru.Openshift.Common.Models;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel.Serialization;

namespace Uhuru.Openshift.Common
{
    public class Node
    {
        public static string GetCartridgeList(bool listDescriptors, bool porcelain, bool oo_debug)
        {
            string output = string.Empty;

            List<Cartridge> carts = CartridgeRepository.Instance.LatestVersions;

            if (porcelain)
            {
                if (listDescriptors)
                {
                    output += "CLIENT_RESULT: ";
                    List<string> descriptors = new List<string>();
                    foreach(Cartridge cart in carts)
                    {
                        dynamic desc = cart.ToDescriptor();
                        StringWriter sw = new StringWriter();
                        Serializer serializer = new Serializer();
                        serializer.Serialize(new Emitter(sw, 2, int.MaxValue, true), desc);
                        descriptors.Add(sw.ToString());
                    }
                    output += JsonConvert.SerializeObject(descriptors);
                }
                else
                {
                    output += "CLIENT_RESULT: ";
                    List<string> names = new List<string>();
                    foreach (Cartridge cart in carts)
                    {
                        names.Add(cart.Name);
                    }
                    output += JsonConvert.SerializeObject(names);
                }
            }
            else
            {
                if (listDescriptors)
                {
                    foreach (Cartridge cart in carts)
                    {
                        dynamic desc = cart.ToDescriptor();
                        StringWriter sw = new StringWriter();
                        Serializer serializer = new Serializer(SerializationOptions.JsonCompatible);
                        serializer.Serialize(new Emitter(sw, 2, int.MaxValue, true), desc);
                        output += string.Format("Cartridge name: {0}\n\nDescriptor:\n {1}\n\n\n", cart.Name, sw.ToString());
                    }
                }
                else
                {
                    output += "Cartridges:\n";
                    foreach (Cartridge cart in carts)
                    {
                        output += string.Format("\t{0}\n", cart.Name);
                    }
                }
            }

            return output;
        }
    }
}
