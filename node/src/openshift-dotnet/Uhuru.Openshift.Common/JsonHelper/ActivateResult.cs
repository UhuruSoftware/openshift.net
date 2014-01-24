using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Common.JsonHelper
{
    public class ActivateResult
    {
        public string Status { get; set; }
        public string GearUuid { get; set; }
        public string DeploymentId { get; set; }
        public List<string> Messages { get; set; }
        public List<string> Errors { get; set; }
    }
}
