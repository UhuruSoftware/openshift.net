using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime
{
    public class UserShellTrap
    {
        private void GetGearEnvVariables()
        {
            Dictionary<string, string> envVariables = new Dictionary<string, string>()
            {
                { "OPENSHIFT_SECRET_TOKEN", "secret" },
                { "OPENSHIFT_GEAR_MEMORY_MB", "512" },
                { "OPENSHIFT_DEPLOYMENTS_DIR", "/var/lib/openshift/528faca7ab631c9ea400000e/app-deployments/" },
                { "OPENSHIFT_TMP_DIR", "/tmp/" },
                { "OPENSHIFT_REPO_DIR", "/var/lib/openshift/528faca7ab631c9ea400000e/app-root/runtime/repo/" },
                { "OPENSHIFT_HOMEDIR", "/var/lib/openshift/528faca7ab631c9ea400000e/" },
                { "OPENSHIFT_GEAR_NAME", "dietate" },
                { "OPENSHIFT_APP_SSH_PUBLIC_KEY", "/var/lib/openshift/528faca7ab631c9ea400000e/.openshift_ssh/id_rsa.pub" },
                { "OPENSHIFT_CLOUD_DOMAIN", "example.com" },
                { "OPENSHIFT_BUILD_DEPENDENCIES_DIR", "/var/lib/openshift/528faca7ab631c9ea400000e/app-root/runtime/build-dependencies/" },
                { "OPENSHIFT_APP_DNS", "dietate-uhuru.openshift.local" },
                { "OPENSHIFT_PRIMARY_CARTRIDGE_DIR", "/var/lib/openshift/528faca7ab631c9ea400000e/ruby/" },
                { "OPENSHIFT_GEAR_DNS", "dietate-uhuru.openshift.local" },
                { "OPENSHIFT_CARTRIDGE_SDK_BASH", "/usr/lib/openshift/cartridge_sdk/bash/sdk" },
                { "OPENSHIFT_APP_SSH_KEY", "/var/lib/openshift/528faca7ab631c9ea400000e/.openshift_ssh/id_rsa" },
                { "OPENSHIFT_DEPENDENCIES_DIR", "/var/lib/openshift/528faca7ab631c9ea400000e/app-root/runtime/dependencies/" },
                { "OPENSHIFT_APP_NAME", "dietate" },
                { "OPENSHIFT_DATA_DIR", "/var/lib/openshift/528faca7ab631c9ea400000e/app-root/data/" },
                { "OPENSHIFT_GEAR_UUID", "528faca7ab631c9ea400000e" },
                { "OPENSHIFT_NAMESPACE", "uhuru" },
                { "OPENSHIFT_BROKER_HOST", "localhost" },
                { "OPENSHIFT_APP_UUID", "528faca7ab631c9ea400000e" },
            };
        }


    }
}
