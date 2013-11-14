using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel.Serialization;

namespace Uhuru.Openshift.Common.Models
{
    public class Profile
    {
        public string Name { get; set; }
        public bool Generated { get; set; }
        public List<object> Provides { get; set; }
        public List<object> StartOrder { get; set; }
        public List<object> StopOrder { get; set; }
        public List<object> ConfigureOrder { get; set; }
        public List<Component> Components { get; set; }
        public List<Connection> Connections { get; set; }
        public Dictionary<string, Component> ComponentNameMap { get; set; }
        public List<object> GroupOverrides { get; set; }

        public Profile()
        {
            this.Provides = new List<object>();
            this.StartOrder = new List<object>();
            this.StopOrder = new List<object>();
            this.ConfigureOrder = new List<object>();
            this.Components = new List<Component>();
            this.Connections = new List<Connection>();
            this.GroupOverrides = new List<object>();
            this.ComponentNameMap = new Dictionary<string, Component>();
        }

        public static Profile FromDescriptor(Cartridge cartridge, dynamic spec)
        {
            Profile profile = new Profile();
            if (spec.ContainsKey("Provides"))
            {
                if (spec["Provides"] is String)
                {
                    profile.Provides = new List<object>() { spec["Provides"] };
                }
                else
                {
                    profile.Provides = spec["Provides"];
                }
            }
            else
            {
                profile.Provides = new List<object>() { };
            }
            if (spec.ContainsKey("Start-Order"))
            {
                if (spec["Start-Order"] is String)
                {
                    profile.StartOrder = new List<object>() { spec["Start-Order"] };
                }
                else
                {
                    profile.StartOrder = spec["Start-Order"];
                }
            }
            else
            {
                profile.StartOrder = new List<object>() { };
            }

            if (spec.ContainsKey("Stop-Order"))
            {
                if (spec["Stop-Order"] is String)
                {
                    profile.StopOrder = new List<object>() { spec["Stop-Order"] };
                }
                else
                {
                    profile.StopOrder = spec["Stop-Order"];
                }
            }
            else
            {
                profile.StopOrder = new List<object>() { };
            }
            if (spec.ContainsKey("Configure-Order"))
            {
                if (spec["Configure-Order"] is String)
                {
                    profile.ConfigureOrder = new List<object>() { spec["Configure-Order"] };
                }
                else
                {
                    profile.ConfigureOrder = spec["Configure-Order"];
                }
            }
            else
            {
                profile.ConfigureOrder = new List<object>() { };
            }

            if (((Dictionary<object, object>)spec).ContainsKey("Components"))
            {
                foreach (dynamic c in spec["Components"])
                {
                    KeyValuePair<string, dynamic> pair = (KeyValuePair<string, dynamic>)c;
                    Component component = Component.FromDescriptor(profile, pair.Value);
                    component.Name = pair.Key;
                    profile.Components.Add(component);
                    profile.ComponentNameMap[component.Name] = component;
                }
            }
            else
            {
                Component component = Component.FromDescriptor(profile, spec);
                component.Generated = true;
                profile.Components.Add(component);
                profile.ComponentNameMap[component.Name] = component;
            }

            if (((Dictionary<object, object>)spec).ContainsKey("Connections"))
            {
                foreach (dynamic c in spec["Connections"])
                {
                    KeyValuePair<string, dynamic> pair = (KeyValuePair<string, dynamic>)c;
                    profile.Connections.Add(Connection.FromDescriptor(pair.Key, pair.Value));
                }
            }

            if (((Dictionary<object, object>)spec).ContainsKey("Group-Overrides"))
            {
                foreach (dynamic c in spec["Group-Overrides"])
                {
                    profile.GroupOverrides.Add(c);
                }
            }

            return profile;
        }

        public dynamic ToDescriptor()
        {
            Dictionary<object, object> h = new Dictionary<object, object>();
            if (this.Provides.Count > 0)
                h["Provides"] = this.Provides;
            if (this.StartOrder.Count > 0)
                h["Start-Order"] = this.StartOrder;
            if (this.StopOrder.Count > 0)
                h["Stop-Order"] = this.StopOrder;
            if (this.ConfigureOrder.Count > 0)
                h["Configure-Order"] = this.ConfigureOrder;
            if (this.Components.Count == 1 && this.Components[0].Generated)
            {
                dynamic comp = this.Components[0].ToDescriptor();
                ((Dictionary<object, object>)comp).Remove("Name");
                foreach (KeyValuePair<object, object> pair in comp)
                {
                    h[pair.Key] = comp[pair.Key];
                }
            }
            else
            {
                Dictionary<string, object> comp = new Dictionary<string, object>();
                foreach(Component com in this.Components)
                {
                    comp[com.Name] = com.ToDescriptor();
                }
                h["Components"] = comp;
            }
            if (this.Connections.Count != 0)
            {
                Dictionary<string, object> conns = new Dictionary<string, object>();
                foreach (Connection con in this.Connections)
                {
                    conns[con.Name] = con.ToDescriptor();
                }
                h["Connections"] = conns;
            }
            h["Group-Overrides"] = this.GroupOverrides;

            return h;
        }
    }
}
