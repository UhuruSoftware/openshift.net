using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Utilities;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel.Serialization;

namespace Uhuru.Openshift.Runtime.Utils
{
    public class Facter
    {
        public static RubyHash GetFacterFacts()
        {
            string rubyLocation = NodeConfig.Values["RUBY_LOCATION"];

            string rubyBinDir = Path.Combine(rubyLocation, "bin");
            string facterScript = Path.Combine(rubyBinDir, "facter.bat");

            ProcessResult result = ProcessExtensions.RunCommandAndGetOutput("cmd.exe", string.Format(@"/c ""{0}"" -j", facterScript), rubyBinDir);

            if (result.ExitCode != 0)
            {
                Logger.Error("Error running facter: rc={0}; stdout={1}; stderr={2}", result.ExitCode, result.StdOut, result.StdErr);
                throw new Exception(string.Format("Error running facter: rc={0}; stdout={1}; stderr={2}", result.ExitCode, result.StdOut, result.StdErr));
            }

            string facterJson = result.StdOut;

            return JsonConvert.DeserializeObject<RubyHash>(facterJson);
        }

        public static RubyHash GetOpenshiftFacts()
        {
            RubyHash nodeUtilization = Node.NodeUtilization();

            return new RubyHash()
                {
                    {"district_uuid", DistrictConfig.Exists() ? DistrictConfig.Values["uuid"] : "NONE" },
                    {"district_active", DistrictConfig.Exists() ? (DistrictConfig.Values["active"] == "true") : false },
                    {"district_first_uid", DistrictConfig.Exists() ? DistrictConfig.Values["first_uid"] : "1000" },
                    {"district_max_uid", DistrictConfig.Exists() ? DistrictConfig.Values["max_uid"] : "6999"},

                    {"ipaddress_eth0", NodeConfig.Values["PUBLIC_IP"]},
                    {"public_ip", NodeConfig.Values["PUBLIC_IP"]},
                    {"public_hostname", NodeConfig.Values["PUBLIC_HOSTNAME"]},

                    {"node_profile", nodeUtilization["node_profile"]},
                    {"max_active_gears", nodeUtilization["max_active_gears"]},
                    {"no_overcommit_active", nodeUtilization["no_overcommit_active"]},
                    {"quota_blocks", nodeUtilization["quota_blocks"]},
                    {"quota_files", nodeUtilization["quota_files"]},
                    {"gears_active_count", nodeUtilization["gears_active_count"]},
                    {"gears_total_count", nodeUtilization["gears_total_count"]},
                    {"gears_idle_count", nodeUtilization["gears_idled_count"]},
                    {"gears_stopped_count", nodeUtilization["gears_stopped_count"]},
                    {"gears_started_count", nodeUtilization["gears_started_count"]},
                    {"gears_deploying_count", nodeUtilization["gears_deploying_count"]},
                    {"gears_unknown_count", nodeUtilization["gears_unknown_count"]},
                    {"gears_usage_pct", nodeUtilization["gears_usage_pct"]},
                    {"gears_active_usage_pct", nodeUtilization["gears_active_usage_pct"]},

                    {"git_repos", nodeUtilization["git_repos_count"]},
                    {"capacity", nodeUtilization["capacity"]},
                    {"active_capacity", nodeUtilization["active_capacity"]},
                    {"sshfp", ""},
                };
        }

        public static void WriteFactsFile(string file)
        {
            try
            {
                RubyHash common = Facter.GetFacterFacts();
                RubyHash nodeSpecific = Facter.GetOpenshiftFacts();

                dynamic desc = common.Merge(nodeSpecific);
                using (StringWriter sw = new StringWriter())
                {
                    Serializer serializer = new Serializer();
                    serializer.Serialize(new Emitter(sw, 2, int.MaxValue, true), desc);

                    File.WriteAllText(file, sw.ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error while generating facts file: {0} - {1}", ex.Message, ex.StackTrace);
                throw;
            }
        }
    }
}
