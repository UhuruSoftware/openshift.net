using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.Common.OODiagnostics
{
    public static class Output
    {

        public static bool verbose = false;


        public static void WriteInfo(string text)
        {
            Console.WriteLine(String.Format("INFO: {0}", text));
        }

        public static void WriteDebug(string text)
        {
            if (verbose)
            {
               
                Console.WriteLine(String.Format("DEBUG: {0}", text));
            }
        }


        public static void WriteWarn(string text)
        {
            WriteWarning(String.Format("WARN: {0}", text));
        }

        public static void WriteWarning(string text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
            Console.ResetColor();

        }

        

        public static void WriteFail(string text)
        {
            
            WriteError(String.Format("FAIL: {0}", text));
            
        }

        public static void WriteError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            Console.ResetColor();
        }

    }
}
