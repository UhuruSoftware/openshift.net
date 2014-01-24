using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Utilities
{
    public class RubyCompatibility
    {
        public static int DateTimeToEpochSeconds(DateTime date)
        {
            return (int)(date - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }
    }
}
