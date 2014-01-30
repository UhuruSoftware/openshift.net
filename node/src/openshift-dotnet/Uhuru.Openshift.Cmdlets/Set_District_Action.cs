using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Config;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("Set", "District-Action")]
    public class Set_District_Action : System.Management.Automation.Cmdlet
    {
        [Parameter]
        public string Uuid;

        [Parameter]
        public string Active;

        [Parameter]
        public string FirstUid;

        [Parameter]
        public string MaxUid;

        protected override void ProcessRecord()
        {
            ReturnStatus returnStatus = new ReturnStatus();

            try
            {
                NodeConfig config = new NodeConfig();

                string distrinctHome = Path.Combine(config.Get("GEAR_BASE_DIR"), ".settings");

                if (!Directory.Exists(distrinctHome))
                {
                    Directory.CreateDirectory(distrinctHome);
                }

                File.WriteAllText(Path.Combine(distrinctHome, "district.info"),
                    string.Format("#Do not  modify manually!\nuuid='{0}'\nactive='{1}'\nfirst_uid={2}\nmax_uid={3}", Uuid, Active, FirstUid, MaxUid));

                //TODO handle profiling

                returnStatus.Output = string.Format("created/updated district {0} with active = {1}, first_uid = {2}, max_uid = {3}", Uuid, Active, FirstUid, MaxUid);
                returnStatus.ExitCode = 0; 
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                returnStatus.Output = ex.ToString();
                returnStatus.ExitCode = 255;
            }
            WriteObject(returnStatus);
        }
    }
}
