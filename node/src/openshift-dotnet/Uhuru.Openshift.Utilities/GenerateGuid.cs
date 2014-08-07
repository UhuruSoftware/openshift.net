using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Utilities
{
    public class PrisonIdConverter
    {
        public static Guid Generate(string uuid)
        {
            return Guid.Parse(uuid.PadLeft(32, '0'));
        }
    }
}
