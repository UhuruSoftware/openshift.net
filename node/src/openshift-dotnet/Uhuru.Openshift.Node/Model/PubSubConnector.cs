using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime.Model
{
    class PubSubConnector
    {
        public string Name
        {
            get;
            set;
        }

        public string ConnectionType
        {
            get;
            set;
        }

        private Dictionary<string, string> ReservedList = new Dictionary<string, string>()
        {
            { "publish-gear-endpoint", "NET_TCP:gear-endpoint-info" },
            { "publish-http-url", "NET_TCP:httpd-proxy-info" },
            { "set-gear-endpoints", "NET_TCP:gear-endpoint-info" }
        };

        public PubSubConnector(string connectionType, string name)
        {
            this.Name = name;
            this.ConnectionType = connectionType;
        }

        public override string ToString()
        {
            return this.Name;
        }

        public bool Reserved
        {
            get
            {
                return this.ReservedList.ContainsKey(this.Name) && this.ReservedList[this.Name] == this.ConnectionType;
            }
        }

        public string ActioName
        {
            get
            {
                return string.Join(string.Empty, this.Name.Split('-').Select(w => char.ToUpper(w[0]) + w.Substring(1)));
            }
        }
    }
}
