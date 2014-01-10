using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Runtime.Utils
{
    class Sdk
    {
        public const string MESSAGE = "message";
        public const string ERROR = "error";

        public static string TranslateOutForClient(string output, string type = MESSAGE)
        {
            StringBuilder result = new StringBuilder();
            if (string.IsNullOrEmpty(output))
            {
                return string.Empty;
            }
            string suffix = string.Empty;
            if (type == ERROR)
            {
                suffix = "ERROR";
            }
            else
            {
                suffix = "MESSAGE";
            }
            foreach (string line in output.Split(new string[] {Environment.NewLine}, StringSplitOptions.None))
            {
                if (line.StartsWith("CLIENT_"))
                {
                    result.AppendLine(line);
                }
                else
                {
                    result.AppendLine(string.Format("CLIENT_{0}: {1}", suffix, line));
                }
            }
            return result.ToString();
        }
    }
}
