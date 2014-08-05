using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Uhuru.Openshift.Cmdlets;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.OOCmd
{
    class Program
    {
        static int Main(string[] args)
        {            
            // trim all args first
            args = args.Select(arg => arg.Trim().Replace("\"",string.Empty)).ToArray();

            for(int i=0;i<args.Length;i++)
            {
                if (args[i].Count(Char.IsWhiteSpace) > 1)
                {
                    var parameters = args[i].Split(' ');
                    args = args.Where(w => w != args[i]).ToArray();
                    args = args.Concat(parameters).ToArray();
                }
            }

            ReturnStatus status = new ReturnStatus();
            try
            {
                Logger.Debug("Running {0}", string.Join(" ", args));

                string method = args[0];

                string className = string.Format("{0}.{1}, {2}",
                    "Uhuru.Openshift.Cmdlets",
                    Regex.Replace(method, @"^(OO|[A-Z])|_[A-Z]|-[A-Z]", m => m.ToString().ToUpper(), RegexOptions.IgnoreCase).Replace("-", "_").Trim(),
                    typeof(ReturnStatus).Assembly.GetName());

                Type t = Type.GetType(className);
                if (t != null)
                {
                    var instance = Activator.CreateInstance(t);
                    Dictionary<string, object> arguments = null;
                    if (args.Length == 1)
                    {
                        string arg = Console.ReadLine();
                        Logger.Debug("Arguments from json {0}",arg);
                        arguments = JsonConvert.DeserializeObject<RubyHash>(arg);                        
                    }
                    else
                    {
                        string[] arg = args.Skip(1).ToArray();
                        arguments = new Arguments(arg).ToDictionary(pair => pair.Key, pair => (object)pair.Value);
                    }
                    switch (method.ToLower())
                    {
                        case "gear":
                            {
                                arguments[args[1]] = true;
                                switch (args[1].ToLower())
                                {

                                    case "activate":
                                        {
                                            arguments["DeploymentId"] = args[2];
                                            break;
                                        }
                                    case "build":
                                        {
                                            if (args.Length > 2)
                                            {
                                                arguments["RefId"] = args[2];
                                            }
                                            break;
                                        }
                                    default:
                                        break;
                                }
                                break;
                            }
                        case "oo-admin-ctl-gears":
                            {
                                arguments["Operation"] = args[1];
                                arguments["UUID"] = args.Skip(2).ToArray();
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                    SetInstanceFields(instance, arguments);
                    MethodInfo process = t.GetMethod("Execute");
                    status = (ReturnStatus)process.Invoke(instance, new object[] { });
                }
                else
                {
                    status.Output = "Command not found";
                    status.ExitCode = 1;
                }
                
            }
            catch(Exception ex)
            {
                status.Output = ex.ToString();
                status.ExitCode = 1;
            }
            Logger.Debug("Finished running {0}. Output: {1}", string.Join(" ", args), status.Output);
            Console.WriteLine(status.Output);
            return status.ExitCode;
        }

        private static string GetFieldName(string arg)
        {
            string result = Regex.Replace(arg, @"^(--[A-Z]|[A-Z])|-[A-Z]|_[A-Z]", m => m.ToString().ToUpper(), RegexOptions.IgnoreCase).Replace("-", "").Replace("_", "").Trim();            
            return result;
        }

        private static void SetInstanceFields(object instance, Dictionary<string, object> args)
        {            
            foreach(KeyValuePair<string, object> pair in args)
            {
                if(pair.Value is JObject)
                {
                    SetInstanceFields(instance, ((JObject)pair.Value).ToObject<RubyHash>());
                }
                else
                {                   
                    Type classType = instance.GetType();
                    string fieldName = GetFieldName(pair.Key);                  
                    if (pair.Value == null)
                    {
                        continue;
                    }
                    Logger.Debug("Computed field name {0} from arg {1} with value {2}", fieldName, pair.Key, pair.Value.ToString());
                    FieldInfo fi = classType.GetField(fieldName);
                    if (fi != null)
                    {
                        if (fi.FieldType == typeof(bool))
                        {
                            bool value = bool.Parse(pair.Value.ToString());
                            fi.SetValue(instance, value);
                        }
                        else if (fi.FieldType == typeof(int))
                        {
                            int value = int.Parse(pair.Value.ToString());
                            fi.SetValue(instance, value);
                        }
                        else if (fi.FieldType == typeof(System.Management.Automation.SwitchParameter))
                        {
                            fi.SetValue(instance, System.Management.Automation.SwitchParameter.Present);
                        }
                        else if(fi.FieldType == typeof(string))
                        {
                            fi.SetValue(instance, pair.Value.ToString());
                        }
                        else if (fi.FieldType == typeof(System.Single))
                        {
                            fi.SetValue(instance, Convert.ToSingle(pair.Value.ToString()));
                        }
                        else
                        {
                            fi.SetValue(instance, pair.Value);
                        }
                    }
                    else
                    {
                        throw new MissingFieldException(string.Format("Field {0} not found", fieldName));
                    }
                }
            }

        }
    }
}
