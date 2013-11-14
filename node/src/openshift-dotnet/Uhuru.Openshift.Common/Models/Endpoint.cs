using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Common.Models
{
    public class Endpoint
    {
        public class Mapping
        {
            public string Frontend { get; set; }
            public string Backend { get; set; }
            public dynamic Options { get; set; }

            public static Mapping FromDescriptor(dynamic spec)
            {
                Mapping mapping = new Mapping();
                mapping.Backend = spec.ContainsKey("Backend") ? spec["Backend"] : null;
                mapping.Frontend = spec.ContainsKey("Frontend") ? spec["Frontend"] : null;
                mapping.Options = spec.ContainsKey("Options") ? spec["Options"] : null;
                return mapping;
            }

            public dynamic ToDescriptor()
            {
                Dictionary<object, object> h = new Dictionary<object, object>();
                if (this.Backend != null)
                {
                    h["Backend"] = this.Backend;
                }
                if (this.Frontend != null)
                {
                    h["Frontend"] = this.Frontend;
                }
                if (this.Options != null)
                {
                    h["Options"] = this.Options;
                }
                return h;
            }
        }

        public string PrivateIpName { get; set; }
        public string PrivatePortName { get; set; }
        public string PrivatePort { get; set; }
        public string PublicPortName { get; set; }
        public string WebsocketPortName { get; set; }
        public string WebsocketPort { get; set; }
        public string Options { get; set; }
        public string Description { get; set; }
        public List<Mapping> Mappings { get; set; }

        public Endpoint()
        {
            this.Mappings = new List<Mapping>();
        }

        public static Endpoint FromDescriptor(dynamic spec)
        {
            Endpoint endpoint = new Endpoint();
            endpoint.PrivateIpName = spec.ContainsKey("Private-IP-Name") ? spec["Private-IP-Name"] : null;
            endpoint.PrivatePortName = spec.ContainsKey("Private-Port-Name") ? spec["Private-Port-Name"] : null;
            endpoint.PrivatePort = spec.ContainsKey("Private-Port") ? spec["Private-Port"] : null;
            endpoint.PublicPortName = spec.ContainsKey("Public-Port-Name") ? spec["Public-Port-Name"] : null;
            endpoint.WebsocketPortName = spec.ContainsKey("WebSocket-Port-Name") ? spec["WebSocket-Port-Name"] : null;
            endpoint.WebsocketPort = spec.ContainsKey("WebSocket-Port") ? spec["WebSocket-Port"] : null;
            endpoint.Options = spec.ContainsKey("Options") ? spec["Options"] : null;
            endpoint.Description = spec.ContainsKey("Description") ? spec["Description"] : null;
            if (((Dictionary<object, object>)spec).ContainsKey("Mappings"))
            {
                foreach (dynamic c in spec["Mappings"])
                {
                    endpoint.Mappings.Add(Mapping.FromDescriptor(c));
                }
            }
            return endpoint;
        }

        public dynamic ToDescriptor()
        {
            Dictionary<object, object> h = new Dictionary<object, object>();
            if (this.PrivateIpName != null)
            {
                h["Private-IP-Name"] = this.PrivateIpName;
            }
            if (this.PrivatePortName != null)
            {
                h["Private-Port-Name"] = this.PrivatePortName;
            }
            if (this.PrivatePort != null)
            {
                h["Private-Port"] = this.PrivatePort;
            }
            if (this.PublicPortName != null)
            {
                h["Public-Port-Name"] = this.PublicPortName;
            }
            if (this.WebsocketPortName != null)
            {
                h["WebSocket-Port-Name"] = this.WebsocketPortName;
            }
            if (this.WebsocketPort != null)
            {
                h["WebSocket-Port"] = this.WebsocketPort;
            }
            if (this.Options != null)
            {
                h["Options"] = this.Options;
            }
            if (this.Description != null)
            {
                h["Description"] = this.Description;
            }
            if (this.Mappings.Count > 0)
            {
                List<object> maps = new List<object>();
                foreach (Mapping map in this.Mappings)
                {
                    maps.Add(map.ToDescriptor());
                }
                h["Mappings"] = maps;
            }

            return h;
        }
    }
}
