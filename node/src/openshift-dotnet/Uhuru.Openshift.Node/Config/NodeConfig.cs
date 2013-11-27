using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime.Config
{
    public class NodeConfig : Uhuru.Openshift.Common.Config
    {
        public static string ConfigDir
        {
            get
            {
                return Environment.GetEnvironmentVariable("OPENSHIFT_CONF_DIR") ?? @"c:\openshift\";
            }
        }

        public static string NodeConfigFile
        {
            get
            {
                return Path.Combine(NodeConfig.ConfigDir, "node.conf");
            }
        }

        private static NodeConfig nodeConfig = null;
        private static readonly object nodeConfigLock = new object();

        public NodeConfig()
            : base(NodeConfig.NodeConfigFile)
        {
        }

        public static NodeConfig Values
        {
            get
            {
                if (nodeConfig == null)
                {
                    lock (nodeConfigLock)
                    {
                        if (nodeConfig == null)
                        {
                            nodeConfig = new NodeConfig();
                        }
                    }
                }

                return nodeConfig;
            }
        }
    }
}
