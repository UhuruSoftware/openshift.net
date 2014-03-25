using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Common.OODiagnostics
{
    class Program
    {

       
        static int waitTime = 0;

        static void Main(string[] args)
        {

            List<ITest> testsToRun = new List<ITest>();

            if (args.Contains("-v") || args.Contains("--verbose"))
            {
                Output.verbose = true; 
            }

            if (args.Contains("-h") || args.Contains("--help"))
            {
                ShowHelp();
                return;
            }

            if (args.Contains("-w"))
            {
                int index = Array.IndexOf(args, "-w");
                waitTime = int.Parse(args[index + 1]) * 1000;
            }

            if (args.Contains("--wait"))
            {
                int index = Array.IndexOf(args, "--wait");
                waitTime = int.Parse(args[index + 1]) * 1000;
            }

            foreach (ITest test in GetAllTests())
            {
                if (args.Contains(test.GetName()))
                {
                    testsToRun.Add(test);
                }
            }

            if (testsToRun.Count() == 0)
            {
                testsToRun = GetAllTests();
                
            }

            foreach (ITest test in testsToRun)
            {
                test.Run();
                Thread.Sleep(waitTime);
            }

            WriteSummary(testsToRun);
        }

        

        public static void ShowHelp()
        {
            StringBuilder helpmessage = new StringBuilder();
            helpmessage.Append("Detect common problems on OpenShift Windows Node");
            helpmessage.AppendLine();
            helpmessage.AppendLine("Usage: oo-diagnostics [switches] [test methods to run]");
            helpmessage.AppendLine("Example: oo-diagnostics");
            helpmessage.AppendLine("Example: oo-diagnostics -v -w 1 test_host_names"); 
            helpmessage.AppendLine();
 

            Output.WriteInfo(helpmessage.ToString());
        }

        private static void WriteSummary(List<ITest> tests)
        {
            int testsWarning = 0;
            int testsFail = 0;
            foreach (ITest test in tests)
            {

                if (test.GetExitCode() == ExitCode.WARNING)
                {
                    testsWarning++;
                }
                else
                {
                    if (test.GetExitCode() == ExitCode.FAIL)
                    {
                        testsFail++;
                    }
                }
            }

            if (testsWarning != 0)
            {
                Output.WriteWarning(string.Format("{0} WARNINGS", testsWarning.ToString()));
            }
            if (testsFail != 0)
            {
                Output.WriteError(string.Format("{0} ERRORS", testsFail.ToString()));
            }
        }

        private static List<ITest> GetAllTests()
        {
            List<ITest> allTests = new List<ITest>();
            var instances = from t in Assembly.GetExecutingAssembly().GetTypes()
                            where t.GetInterfaces().Contains(typeof(ITest))
                                     && t.GetConstructor(Type.EmptyTypes) != null
                            select Activator.CreateInstance(t) as ITest;

            foreach (var instance in instances)
            {
                allTests.Add(instance);
            }

            return allTests;
        }
       

        
    }
}
