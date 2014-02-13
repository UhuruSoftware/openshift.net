using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime.Config
{
    public class DistrictConfig : Uhuru.Openshift.Common.Config
    {
        public static string ConfigDir
        {
            get
            {
                return Environment.GetEnvironmentVariable("OPENSHIFT_CONF_DIR") ?? @"c:\openshift\";
            }
        }

        public static string DistrictConfigFile
        {
            get
            {
                return Path.Combine(DistrictConfig.ConfigDir, "settings", "district.info");
            }
        }

        private static DistrictConfig districtConfig = null;
        private static readonly object districtConfigLock = new object();

        public DistrictConfig()
            : base(DistrictConfig.DistrictConfigFile)
        {
        }

        public static bool Exists()
        {
            return File.Exists(DistrictConfig.DistrictConfigFile);
        }

        public static DistrictConfig Values
        {
            get
            {
                if (districtConfig == null)
                {
                    lock (districtConfigLock)
                    {
                        if (districtConfig == null)
                        {
                            districtConfig = new DistrictConfig();
                        }
                    }
                }

                return districtConfig;
            }
        }
    }
}
