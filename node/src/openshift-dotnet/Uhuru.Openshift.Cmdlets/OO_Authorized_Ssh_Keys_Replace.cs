using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using Uhuru.Openshift.Common.JsonHelper;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Authorized-Ssh-Keys-Replace")]
    public class OO_Authorized_Ssh_Keys_Replace : System.Management.Automation.Cmdlet 
    {
        [Parameter]
        public string WithAppUuid;

        [Parameter]
        public string WithAppName;

        [Parameter]
        public string WithContainerUuid;

        [Parameter]
        public string WithContainerName;

        [Parameter]
        public string WithNamespace;

        [Parameter]
        public string WithRequestId;

        [Parameter]
        public string WithSshKeys;

        [Parameter]
        public string WithExposePorts;

        public int WithUid;

        protected override void ProcessRecord()
        {
            this.WriteObject(Execute());
        }

        public ReturnStatus Execute()
        {
            ReturnStatus status = new ReturnStatus();
            try
            {
                ApplicationContainer container = new ApplicationContainer(WithAppUuid, WithContainerUuid, null, WithAppName, WithContainerName,
                    WithNamespace, null, null, null, WithUid);
                List<SshKey> keys = new List<SshKey>();
                if (!string.IsNullOrWhiteSpace(WithSshKeys))
                {
                    JArray varsArray = (JArray)JsonConvert.DeserializeObject(WithSshKeys);
                    keys = varsArray.ToObject<List<SshKey>>();
                }
                container.ReplaceSshKeys(keys);
                status.ExitCode = 0;
                status.Output = string.Empty;
            }
            catch(Exception ex)
            {
                Logger.Error("Error running oo-authorized-ssh-keys-replace command: {0} - {1}", ex.Message, ex.StackTrace);
                status.ExitCode = 1;
                status.Output = ex.Message;
            }
            return status;
        }
    }
}
