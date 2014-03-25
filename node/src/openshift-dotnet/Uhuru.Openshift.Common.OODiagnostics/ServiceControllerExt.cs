using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Common.OODiagnostics
{
    public class ServiceControllerExt : ServiceController
    {
        public ServiceControllerExt()
            : base()
        { }
        public ServiceControllerExt(string name)
            : base(name)
        { }
        public ServiceControllerExt(string name, string machineName)
            : base(name, machineName)
        { }


        public string GetStartupType()
        {
            if (this.ServiceName != null)
            {
                //construct the management path
                string path = "Win32_Service.Name='" + this.ServiceName + "'";
                ManagementPath p = new ManagementPath(path);
                //construct the management object
                ManagementObject ManagementObj = new ManagementObject(p);
                return ManagementObj["StartMode"].ToString();
            }
            else
            {
                return null;
            }
        }
    }
}
