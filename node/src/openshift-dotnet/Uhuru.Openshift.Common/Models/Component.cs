using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Common.Models
{
    public class Component
    {
        public string Name { get; set; }
        public bool Generated { get; set; }
        public List<Connector> Publishes { get; set; }
        public List<Connector> Subscribes { get; set; }
        public Scaling Scaling { get; set; }

        public Component()
        {
            this.Publishes = new List<Connector>();
            this.Subscribes = new List<Connector>();
        }

        public static Component FromDescriptor(Profile profile, dynamic spec)
        {
            Component component = new Component();
            component.Name = spec["Name"];
            if (((Dictionary<object, object>)spec).ContainsKey("Publishes"))
            {
                foreach (dynamic c in spec["Publishes"])
                {
                    KeyValuePair<object, dynamic> pair = (KeyValuePair<object, dynamic>)c;
                    Connector connector = Connector.FromDescriptor(pair.Key.ToString(), pair.Value);
                    component.Publishes.Add(connector);
                }
            }

            if (((Dictionary<object, object>)spec).ContainsKey("Subscribes"))
            {
                foreach (dynamic c in spec["Subscribes"])
                {
                    KeyValuePair<object, dynamic> pair = (KeyValuePair<object, dynamic>)c;
                    Connector connector = Connector.FromDescriptor(pair.Key.ToString(), pair.Value);
                    component.Subscribes.Add(connector);
                }
            }

            component.Scaling = spec.ContainsKey("Scaling") ? Scaling.FromDescriptor(spec) : null;
            return component;
        }

        public dynamic ToDescriptor()
        {
            Dictionary<object, object> p = new Dictionary<object, object>();
            
            foreach (Connector v in this.Publishes)
            {
                p[v.Name] = v.ToDescriptor();
            }
            Dictionary<object, object> s = new Dictionary<object, object>();
            foreach (Connector v in this.Subscribes)
            {
                s[v.Name] = v.ToDescriptor();
            }
            Dictionary<object, object> h = new Dictionary<object, object>();
            h["Publishes"] = p;
            h["Subscribes"] = s;
            if (this.Scaling != null)
            {
                h["Scaling"] = this.Scaling.ToDescriptor();
            }
            return h;
        }
    }
}
