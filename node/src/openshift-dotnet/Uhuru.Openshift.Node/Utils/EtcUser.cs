using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime.Utils
{
    public class EtcUser
    {
        public string Name {get;set;}
        public int Uid { get; set; }
        public int Gid { get; set; }
        public string Passwd { get; set; }
        public string Gecos { get; set; }
        public string Dir { get; set; }
        public string Shell { get; set; }

        public EtcUser()
        {

        }
    }
}
