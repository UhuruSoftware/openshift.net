using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Uhuru.Openshift.Common.Models;
using Uhuru.Openshift.Utilities;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel.Serialization;

namespace Uhuru.Openshift.Runtime
{
    public static class Node
    {
        private static Common.Config resourceLimitsCache = null;
        private static readonly object resourceLimitsCacheLock = new object();

        private const string DEFAULT_NODE_PROFILE = "small";
        private const string DEFAULT_QUOTA_BLOCKS = "1048576";
        private const string DEFAULT_NO_OVERCOMMIT_ACTIVE = "false";
        private const string DEFAULT_MAX_ACTIVE_GEARS = "0";
        private const string DEFAULT_QUOTA_FILES = "80000";

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
                    foreach (Cartridge cart in carts)
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

        public static RubyHash NodeUtilization()
        {
            RubyHash result = new RubyHash();

            Common.Config resource = Node.ResourceLimits;

            if (resource == null)
            {
                return result;
            }

            result["node_profile"] = resource.Get("node_profile", Node.DEFAULT_NODE_PROFILE);
            result["quota_blocks"] = resource.Get("quota_blocks", Node.DEFAULT_QUOTA_BLOCKS);
            result["quota_files"] = resource.Get("quota_files", Node.DEFAULT_QUOTA_FILES);
            result["no_overcommit_active"] = Boolean.Parse(resource.Get("no_overcommit_active", Node.DEFAULT_NO_OVERCOMMIT_ACTIVE));


            result["max_active_gears"] = Convert.ToInt32(resource.Get("max_active_gears", Node.DEFAULT_MAX_ACTIVE_GEARS));


            //
            // Count number of git repos and gear status counts
            //
            result["git_repos_count"] = 0;
            result["gears_total_count"] = 0;
            result["gears_idled_count"] = 0;
            result["gears_stopped_count"] = 0;
            result["gears_started_count"] = 0;
            result["gears_deploying_count"] = 0;
            result["gears_unknown_count"] = 0;

            
            foreach (ApplicationContainer app in ApplicationContainer.All(null, false))
            {
                result["gears_total_count"] += 1;

                switch (app.State.Value())
                {
                    case "building":
                    case "deploying":
                    case "new":
                        result["gears_deploying_count"] += 1;
                        break;
                    case "started":
                        result["gears_started_count"] += 1;
                        break;
                    case "idle":
                        result["gears_idled_count"] += 1;
                        break;
                    case "stopped":
                        result["gears_stopped_count"] += 1;
                        break;
                    case "unknown":
                        result["gears_unknown_count"] += 1;
                        break;
                }
            }


            // consider a gear active unless explicitly not
            result["gears_active_count"] = result["gears_total_count"] - result["gears_idled_count"] - result["gears_stopped_count"];
            result["gears_usage_pct"] = result["max_active_gears"] == 0 ? 0.0f : (result["gears_total_count"] * 100.0f) / result["max_active_gears"];
            result["gears_active_usage_pct"] = result["max_active_gears"] == 0 ? 0.0f : (result["gears_active_count"] * 100.0f) / result["max_active_gears"];
            result["capacity"] = result["gears_usage_pct"].ToString();
            result["active_capacity"] = result["gears_active_usage_pct"].ToString();
            return result;
        }


        public static Common.Config ResourceLimits
        {
            get
            {
                if (resourceLimitsCache != null)
                {
                    return resourceLimitsCache;
                }

                lock (resourceLimitsCacheLock)
                {
                    if (resourceLimitsCache == null)
                    {
                        string limitsFile = @"c:\openshift\resource_limits.conf";
                        if (File.Exists(limitsFile))
                        {
                            return new Common.Config(limitsFile);
                        }
                        else
                        {
                            return null;
                        }
                    }

                    return resourceLimitsCache;
                }
            }
        }
    }
}