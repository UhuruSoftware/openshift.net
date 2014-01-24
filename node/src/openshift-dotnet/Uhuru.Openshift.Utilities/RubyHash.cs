using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Utilities
{
    public class RubyHash : Dictionary<string, object>
    {
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
    }
}
