using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Common.Models
{
    public class Connector
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }

        public static Connector FromDescriptor(string name, dynamic spec)
        {
            Connector connector = new Connector();
            connector.Name = name;
            connector.Type = spec["Type"];
            connector.Required = spec.ContainsKey("Required") ? (spec["Required"].ToString() == "true" ? true : false) : false;
            return connector;
        }

        public dynamic ToDescriptor()
        {
            Dictionary<object, object> h = new Dictionary<object, object>();
            h["Type"] = this.Type;
            h["Required"] = this.Required;
            return h;
        }
    }
}
