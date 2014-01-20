using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uhuru.Openshift.Common.Models;
using Uhuru.Openshift.Common.Utils;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Utils;

namespace Uhuru.Openshift.Runtime
{
    public partial class ApplicationContainer
    {
        public string DetermineDeploymentRef(string input=null)
        {
            string refId = input;
            if(string.IsNullOrEmpty(refId))
            {
                if (Environment.GetEnvironmentVariables().Contains("OPENSHIFT_DEPLOYMENT_BRANCH"))
                {
                    refId = Environment.GetEnvironmentVariable("OPENSHIFT_DEPLOYMENT_BRANCH");
                }
                else
                {
                    refId = "master";
                }
            }
            return refId;
        }
    }
}
