using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Common.JsonHelper;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    public class OO_Authorized_Ssh_Key_Batch_Add
    {

        public string WithAppUuid;

        public string WithAppName;

        public string WithExposePorts;

        public string WithContainerUuid;

        public string WithContainerName;

        public string WithNamespace;

        public string WithRequestId;

        public string WithSshKeys;
        
        public int WithUid;

        public ReturnStatus Execute()
        {
            ReturnStatus status = new ReturnStatus();

            try
            {
                ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName,
                   WithContainerName, WithNamespace, null, null, null, WithUid);
                List<SshKey> keys = new List<SshKey>();
                if (!string.IsNullOrWhiteSpace(WithSshKeys))
                {
                    JArray varsArray = (JArray)JsonConvert.DeserializeObject(WithSshKeys);
                    keys = varsArray.ToObject<List<SshKey>>();
                }
                container.AddSshKeys(keys);

            }
            catch (Exception ex)
            {
                Logger.Error("Error running oo-authorized-ssh-key-batch-add command: {0} - {1}", ex.Message, ex.StackTrace);
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }
            return status;
        }
    }
}
