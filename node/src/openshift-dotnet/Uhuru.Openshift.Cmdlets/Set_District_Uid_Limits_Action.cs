using System;
using System.IO;
using System.Management.Automation;
using System.Text.RegularExpressions;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Runtime.Config;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("Set", "District-Uid-Limits-Action")]
    public class Set_District_Uid_Limits_Action : System.Management.Automation.Cmdlet
    {
        [Parameter]
        public string FirstUid;

        [Parameter]
        public string MaxUid;

        protected override void ProcessRecord()
        {
            WriteObject(Execute());
        }

        public ReturnStatus Execute()
        {
            ReturnStatus returnStatus = new ReturnStatus();
            try
            {
                NodeConfig config = new NodeConfig();

                string distrinctInfoPath = Path.Combine(config.Get("GEAR_BASE_DIR"), ".settings", "district.info");

                string districtInfo = File.ReadAllText(distrinctInfoPath);

                districtInfo = Regex.Replace(districtInfo, "first_uid=\\d+",
                    string.Format("first_uid={0}", FirstUid), RegexOptions.Multiline);

                districtInfo = Regex.Replace(districtInfo, "max_uid=\\d+",
                    string.Format("max_uid={0}", MaxUid), RegexOptions.Multiline);

                File.WriteAllText(distrinctInfoPath, districtInfo);

                //TODO handle profiling

                returnStatus.Output = string.Format("updated district uid limits with first_uid = {0}, max_uid = {1}", FirstUid, MaxUid);
                returnStatus.ExitCode = 0;


            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                returnStatus.Output = ex.ToString();
                returnStatus.ExitCode = 255;
            }

            return returnStatus;
        }

    }
}
