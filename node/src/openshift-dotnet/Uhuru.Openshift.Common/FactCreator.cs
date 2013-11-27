using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel.Serialization;

namespace Uhuru.Openshift.Common
{
    public class FactCreator
    {
        private Dictionary<string, object> yamlObject;

        private dynamic ToDescriptor()
        {
            return yamlObject;
        }

        public FactCreator()
            : this(new Dictionary<string, object>())
        {

        }

        public FactCreator(Hashtable initialFacts)
            : this()
        {
            this.yamlObject = new Dictionary<string, object>();

            foreach (string key in initialFacts.Keys)
            {
                this[key] = initialFacts[key];
            }
        }

        public FactCreator(Dictionary<string, object> initialFacts)
        {
            this.yamlObject = initialFacts;
        }

        public object this[string key]
        {
            get 
            {
                return yamlObject[key];
            }
            set 
            {
                yamlObject[key] = value;    
            }
        }

        public string GetYaml()
        {
            dynamic desc = this.ToDescriptor();
            using (StringWriter sw = new StringWriter())
            {
                Serializer serializer = new Serializer();
                serializer.Serialize(new Emitter(sw, 2, int.MaxValue, true), desc);
                return sw.ToString();
            }
        }
    }
}
