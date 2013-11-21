using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Cmdlets
{
    [Cmdlet("OO", "Gear")]
    public class Gear : Cmdlet 
    {
        [Parameter]
        public SwitchParameter Init { get; set; }

        [Parameter]
        public SwitchParameter Prereceive { get; set; }

        [Parameter]
        public SwitchParameter Postreceive { get; set; }
        
        protected override void ProcessRecord()
        {
            string appUuid = Environment.GetEnvironmentVariable("OPENSHIFT_APP_UUID");
            string gearUuid = Environment.GetEnvironmentVariable("OPENSHIFT_GEAR_UUID");
            string appName = Environment.GetEnvironmentVariable("OPENSHIFT_APP_NAME");
            string gearName = Environment.GetEnvironmentVariable("OPENSHIFT_GEAR_NAME");
            string nmSpace = Environment.GetEnvironmentVariable("OPENSHIFT_NAMESPACE");

            ApplicationContainer container = new ApplicationContainer(appUuid, gearUuid, System.Security.Principal.WindowsIdentity.GetCurrent().Name, appName, gearName, nmSpace, null, null, null);

            if (Prereceive)
            {
                Dictionary<string, object> options = new Dictionary<string, object>();
                options["init"] = Init;
                options["hotDeploy"] = true;
                options["forceCleanBuild"] = true;
                options["ref"] = "master";
                container.PreReceive(options);
            }
            else if (Postreceive)
            {
                Dictionary<string, object> options = new Dictionary<string, object>();
                options["init"] = Init;
                options["all"] = true;
                options["reportDeployment"] = true;
                options["ref"] = "master";
                if (Init)
                {
                    container.PostReceive(options);
                }
            }
        }
    }
}
