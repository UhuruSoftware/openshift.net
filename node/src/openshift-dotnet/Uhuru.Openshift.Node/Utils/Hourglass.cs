using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime.Utils
{
    public class Hourglass
    {
        public DateTime EndTime
        {
            get { return endTime; }
            set { endTime = value; }
        }

        private DateTime endTime;
        private DateTime startTime;
        int duration;

        public Hourglass(int duration)
        {
            this.duration = duration;
            this.startTime = DateTime.Now;
            this.endTime = this.startTime.AddSeconds(this.duration);
        }

        public int Elapsed
        {
            get
            {
                return (int)Math.Round(DateTime.Now.Subtract(this.startTime).TotalSeconds, 0);
            }
        }

        public int Remaining
        {
            get
            {
                return Math.Max(0, this.duration - Elapsed);
            }
        }

        public bool Expired
        {
            get
            {
                return Remaining == 0;
            }
        }
    }
}
