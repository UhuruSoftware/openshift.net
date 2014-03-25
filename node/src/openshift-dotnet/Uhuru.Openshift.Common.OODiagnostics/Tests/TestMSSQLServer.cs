using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Common.OODiagnostics.Tests
{
    public class TestMSSQLServer :ITest
    {
        ExitCode exitCode = ExitCode.PASS;
        const string SQL2008REGPATH = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL10_50.MSSQLSERVER\Setup";
        const string SQL2012REGPATH = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL11.MSSQLSERVER2012\Setup";
        
        public string GetName()
        {
            return "test_mssql_server";
        }

        public void Run()
        {
            Output.WriteDebug("Testing MS SQL 2008");
            TestSqlServer(SQL2008REGPATH, "MSSQLSERVER", "Microsfot SQL Server 2008");
            
            Output.WriteDebug("Testing MS SQL 2012");
            TestSqlServer(SQL2012REGPATH, "MSSQLSERVER2012", "Microsfot SQL Server 2012");
        }

        private void TestSqlServer(string regPath, string serviceName, string serverName)
        {
            
            var registryValue = (string)Registry.GetValue(regPath, "", "");
            if (registryValue == null)
            {
                Output.WriteError(string.Format("The {0} is not installed", serverName));
                exitCode = ExitCode.FAIL;
            }
            else
            {
                ServiceControllerExt sc = new ServiceControllerExt(serviceName);
                if (sc.Status != System.ServiceProcess.ServiceControllerStatus.Stopped)
                {
                    Output.WriteError(string.Format("The {0} service {1} is running.", serverName, serviceName));
                    exitCode = ExitCode.FAIL;
                }
                if (sc.GetStartupType().ToLower() != "disabled")
                {
                    Output.WriteError(string.Format("The {0} service {1} is not disabled.", serverName, serviceName));
                    exitCode = ExitCode.FAIL;
                }

            }
        }


        public ExitCode GetExitCode()
        {
            return exitCode;
        }
    }
}
