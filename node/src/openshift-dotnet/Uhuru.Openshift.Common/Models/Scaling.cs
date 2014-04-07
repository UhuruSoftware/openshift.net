using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Common.Models
{
    public class Scaling
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public int MinManaged { get; set; }
        public int Multiplier { get; set; }
        public bool Required { get; set; }

        public bool Generated 
        {
            get
            {
                return (this.MinManaged == 1 && this.Min == 1 && this.Multiplier == 1 && this.Max == -1);
            }
        }

        public Scaling()
        {
            this.Min = 1;
            this.Max = -1;
            this.MinManaged = 1;
            this.Multiplier = 1;
        }

        public static Scaling FromDescriptor(dynamic spec)
        {
            Scaling scaling = new Scaling();
            scaling.Min = spec.ContainsKey("Min") ? spec["Min"] : 1;
            scaling.Max = spec.ContainsKey("Max") ? spec["Max"] : -1;
            scaling.MinManaged = spec.ContainsKey("Min-Managed") ? spec["Min-Managed"] : 1;
            scaling.Multiplier = spec.ContainsKey("Multiplier") ? spec["Multiplier"] : 1;
            scaling.Required = spec.ContainsKey("Required") ? spec["Required"] : true;
            return scaling;
        }

        public dynamic ToDescriptor()
        {
            Dictionary<object, object> h = new Dictionary<object, object>();
            h["Min"] = this.Min;
            h["Max"] = this.Max;
            h["Min-Managed"] = this.MinManaged;
            h["Multiplier"] = this.Multiplier;
            h["Required"] = this.Required;
            return h;
        }
    }
}
