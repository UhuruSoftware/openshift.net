using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Openshift.MsSQLSysGenerator
{
    public static class Output
    {
        public static void WriteError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteInfo(text);
            Console.ResetColor();
        }

        public static void WriteWarning(string text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void WriteSuccess(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            WriteInfo(text);
            Console.ResetColor();
        }

        public static void WriteInfo(string text)
        {
            Console.WriteLine(string.Format("{0}-> {1}", DateTime.Now.ToShortTimeString().ToString(), text));
        }
    }
}
