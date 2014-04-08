using System;
using System.Collections.Generic;

namespace Uhuru.Openshift.Common.Models
{
    public class Cartridge
    {

        public string Id { get; set; }

        public string OriginalName { get; set; }

        public string Version { get; set; }

        public List<object> Versions { get; set; }

        public string Architecture { get; set; }

        public string DisplayName { get; set; }

        public string License { get; set; }

        public string LicenseUrl { get; set; }

        public string Vendor { get; set; }

        public string CartridgeVendor { get; set; }

        public string Description { get; set; }

        public List<object> Provides { get; set; }

        public List<object> Requires { get; set; }

        public List<object> Conflicts { get; set; }

        public List<object> NativeRequires { get; set; }

        public List<object> Categories { get; set; }

        public string Website { get; set; }

        public List<object> Suggests { get; set; }

        public dynamic HelpTopics { get; set; }

        public dynamic CartDataDef { get; set; }

        public List<object> AdditionalControlActions { get; set; }

        public string CartridgeVersion { get; set; }

        public List<Endpoint> Endpoints { get; set; }

        public List<object> StartOrder { get; set; }
        public List<object> StopOrder { get; set; }
        public List<object> ConfigureOrder { get; set; }

        public dynamic Spec { get; set; }

        public string Platform { get; set; }

        public List<Component> Components { get; set; }

        public Dictionary<string, Component> ComponentNameMap { get; set; }

        public List<Connection> Connections { get; set; }

        public List<object> GroupOverrides { get; set; }

        public bool Obsolete { get; set; }

        public Cartridge()
        {
            this.Components = new List<Component>();
            this.ComponentNameMap = new Dictionary<string, Component>();
            this.StartOrder = new List<object>();
            this.StopOrder = new List<object>();
            this.ConfigureOrder = new List<object>();
            Endpoints = new List<Endpoint>();
            this.Connections = new List<Connection>();
            this.GroupOverrides = new List<object>();
        }

        public static Cartridge FromDescriptor(dynamic spec)
        {
            Cartridge cart = new Cartridge();
            
            cart.Spec = spec;
            cart.Id = spec.ContainsKey("Id") ?  spec["Id"] :  "";
            cart.OriginalName = spec["Name"];
            cart.Version = spec.ContainsKey("Version") ? spec["Version"] : "0.0";
            cart.Versions = spec.ContainsKey("Versions") ? spec["Versions"] : new List<object>();
            cart.Architecture = spec.ContainsKey("Architecture") ? spec["Architecture"] : "noarch";
            cart.DisplayName = spec.ContainsKey("Display-Name") ? spec["Display-Name"] : string.Format("{0}-{1}-{2}", cart.OriginalName, cart.Version, cart.Architecture);
            cart.License = spec["License"] ?? "unknown";
            cart.LicenseUrl = spec["License-Url"] ?? "";
            cart.Vendor = spec["Vendor"] ?? "unknown";
            cart.CartridgeVendor = spec["Cartridge-Vendor"] ?? "unknown";
            cart.Description = spec["Description"] ?? "";

            if (spec.ContainsKey("Provides"))
            {
                if (spec["Provides"] is String)
                {
                    cart.Provides = new List<object>() { spec["Provides"] };
                }
                else
                {
                    cart.Provides = spec["Provides"];
                }
            }
            else
            {
                cart.Provides = new List<object>() { };
            }
            if (spec.ContainsKey("Requires"))
            {
                if (spec["Requires"] is String)
                {
                    cart.Requires = new List<object>() { spec["Requires"] };
                }
                else
                {
                    cart.Requires = spec["Requires"];
                }
            }
            else
            {
                cart.Requires = new List<object>() { };
            }
            if (spec.ContainsKey("Conflicts"))
            {
                if (spec["Conflicts"] is String)
                {
                    cart.Conflicts = new List<object>() { spec["Conflicts"] };
                }
                else
                {
                    cart.Conflicts = spec["Conflicts"];
                }
            }
            else
            {
                cart.Conflicts = new List<object>() { };
            }
            if (spec.ContainsKey("Native-Requires"))
            {
                if (spec["Native-Requires"] is String)
                {
                    cart.NativeRequires = new List<object>() { spec["Native-Requires"] };
                }
                else
                {
                    cart.NativeRequires = spec["Native-Requires"];
                }
            }
            else
            {
                cart.NativeRequires = new List<object>() { };
            }
            cart.Categories = spec["Categories"] ?? new List<object>() { };
            cart.Website = spec["Website"] ?? "";
            cart.HelpTopics = spec["Help-Topics"] ?? new object();
            cart.CartDataDef = spec["Cart-Data"] ?? new object();
            cart.AdditionalControlActions = spec.ContainsKey("Additional-Control-Actions") ? spec["Additional-Control-Actions"] : new List<object>() { };
            cart.CartridgeVersion = spec["Cartridge-Version"] ?? "0.0.0";
            cart.Platform = spec["Platform"] ?? "Windows";

            if (((Dictionary<object, object>)spec).ContainsKey("Endpoints"))
            {
                if (spec["Endpoints"] is List<object>)
                {
                    foreach (dynamic ep in spec["Endpoints"])
                    {
                        cart.Endpoints.Add(Endpoint.FromDescriptor(ep, string.Empty));
                    }
                }
            }

            if (spec.ContainsKey("Start-Order"))
            {
                if (spec["Start-Order"] is String)
                {
                    cart.StartOrder = new List<object>() { spec["Start-Order"] };
                }
                else
                {
                    cart.StartOrder = spec["Start-Order"];
                }
            }
            else
            {
                cart.StartOrder = new List<object>() { };
            }

            if (spec.ContainsKey("Stop-Order"))
            {
                if (spec["Stop-Order"] is String)
                {
                    cart.StopOrder = new List<object>() { spec["Stop-Order"] };
                }
                else
                {
                    cart.StopOrder = spec["Stop-Order"];
                }
            }
            else
            {
                cart.StopOrder = new List<object>() { };
            }

            if (spec.ContainsKey("Configure-Order"))
            {
                if (spec["Configure-Order"] is String)
                {
                    cart.ConfigureOrder = new List<object>() { spec["Configure-Order"] };
                }
                else
                {
                    cart.ConfigureOrder = spec["Configure-Order"];
                }
            }
            else
            {
                cart.ConfigureOrder = new List<object>() { };
            }

            if (((Dictionary<object, object>)spec).ContainsKey("Components"))
            {
                foreach (dynamic c in spec["Components"])
                {
                    KeyValuePair<string, dynamic> pair = (KeyValuePair<string, dynamic>)c;
                    Component component = Component.FromDescriptor(cart, pair.Value);
                    component.Name = pair.Key;
                    cart.Components.Add(component);
                    cart.ComponentNameMap[component.Name] = component;
                }
            }
            else
            {
                Component component = Component.FromDescriptor(cart, spec);
                component.Generated = true;
                cart.Components.Add(component);
                cart.ComponentNameMap[component.Name] = component;
            }

            if (((Dictionary<object, object>)spec).ContainsKey("Connections"))
            {
                foreach (dynamic c in spec["Connections"])
                {
                    KeyValuePair<string, dynamic> pair = (KeyValuePair<string, dynamic>)c;
                    cart.Connections.Add(Connection.FromDescriptor(pair.Key, pair.Value));
                }
            }

            if (((Dictionary<object, object>)spec).ContainsKey("Group-Overrides"))
            {
                foreach (dynamic c in spec["Group-Overrides"])
                {
                    cart.GroupOverrides.Add(c);
                }
            }

            cart.Obsolete = spec.ContainsKey("Obsolete") ?  spec["spec"] : false;
            

            return cart;
        }

        public dynamic ToDescriptor()
        {
            Dictionary<object, object> h = new Dictionary<object, object>();
            h["Name"] = this.OriginalName;
            h["Display-Name"] = this.DisplayName;
            h["Id"] = this.Id;
            h["Architecture"] = this.Architecture;
            h["Version"] = this.Version;
            h["Versions"] = this.Versions;
            h["Description"] = this.Description;
            h["License"] = this.License;
            h["License-Url"] = this.LicenseUrl;
            h["Categories"] = this.Categories;
            h["Website"] = this.Website;
            h["HelpTopics"] = this.HelpTopics;
            h["Cart-Data"] = this.CartDataDef;
            h["Platform"] = this.Platform;

            if (this.AdditionalControlActions.Count > 0)
                h["Additional-Control-Actions"] = this.AdditionalControlActions;
            h["Cartridge-Version"] = this.CartridgeVersion;
            if (this.Provides.Count > 0)
                h["Provides"] = this.Provides;
            if (this.Requires.Count > 0)
                h["Requires"] = this.Requires;
            if (this.Conflicts.Count > 0)
                h["Conflicts"] = this.Conflicts;
            if (this.Suggests != null && this.Suggests.Count > 0)
                h["Suggests"] = this.Suggests;
            if (this.NativeRequires.Count > 0)
                h["Native-Requires"] = this.NativeRequires;
            h["Vendor"] = this.Vendor;
            h["Cartridge-Vendor"] = this.CartridgeVendor;

            h["Obsolete"] = this.Obsolete;

            if (this.Endpoints.Count > 0)
            {
                List<object> endps = new List<object>();
                foreach (Endpoint ep in this.Endpoints)
                {
                    endps.Add(ep.ToDescriptor());
                }
                h["Endpoints"] = endps;
            }

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
                foreach (Component com in this.Components)
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

        public string Name
        {
            get
            {
                if (this.CartridgeVendor == "redhat" || this.CartridgeVendor == "uhuru" || string.IsNullOrEmpty(this.CartridgeVendor))
                {
                    return string.Format("{0}-{1}", this.OriginalName, this.Version);
                }
                else
                {
                    return string.Format("{0}-{1}-{2}", this.CartridgeVendor, this.OriginalName, this.Version);
                }
            }
        }

    }
}