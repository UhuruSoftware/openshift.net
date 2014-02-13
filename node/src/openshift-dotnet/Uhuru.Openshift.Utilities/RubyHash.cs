using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Utilities
{
    public class RubyHash : Dictionary<string, object>
    {
        public RubyHash()
            : base()
        {

        }

        public RubyHash(Dictionary<string, object> content)
            : this()
        {
            foreach (var record in content)
            {
                this[record.Key] = record.Value;
            }
        }

        public RubyHash(Dictionary<string, string> content)
            : this()
        {
            foreach (var record in content)
            {
                this[record.Key] = record.Value;
            }
        }

        new public dynamic this[string key]
        {
            get
            {
                if (this.ContainsKey(key))
                {
                    return base[key];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                base[key] = value;
            }
        }

        public RubyHash Merge(RubyHash another)
        {
            RubyHash result = new RubyHash();

            foreach (string key in this.Keys)
            {
                result[key] = this[key];
            }

            foreach (string key in another.Keys)
            {
                result[key] = another[key];
            }

            return result;
        }
        
        public dynamic Delete(string key)
        {
            dynamic result = this[key];
            this.Remove(key);
            return result;
        }

        public RubyHash Merge(Dictionary<string, object> another)
        {
            RubyHash result = new RubyHash();

            foreach (string key in this.Keys)
            {
                result[key] = this[key];
            }

            foreach (string key in another.Keys)
            {
                result[key] = another[key];
            }

            return result;
        }

        public RubyHash Merge(Dictionary<string, string> another)
        {
            RubyHash result = new RubyHash();

            foreach (string key in this.Keys)
            {
                result[key] = this[key];
            }

            foreach (string key in another.Keys)
            {
                result[key] = another[key];
            }

            return result;
        }
    }
}
