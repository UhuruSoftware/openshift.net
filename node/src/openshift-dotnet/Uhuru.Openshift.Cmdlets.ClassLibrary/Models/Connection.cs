using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Common.Models
{
    public class Connection
    {
        public string Name { get; set; }
        public List<object> Components { get; set; }

        public static Connection FromDescriptor(string name, dynamic spec)
        {
            Connection connection = new Connection();
            connection.Name = name;
            connection.Components = spec["Components"];
            return connection;
        }

        public dynamic ToDescriptor()
        {
            Dictionary<object, object> h = new Dictionary<object, object>();
            h["Components"] = this.Components;
            return h;
        }
    }
}
