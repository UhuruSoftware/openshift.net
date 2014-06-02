using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Model;
using Uhuru.Openshift.Runtime.Utils;
using Uhuru.Openshift.Utilities;
using Uhuru.Openshift.Common.Models;
using Uhuru.Openshift.Common.Utils;
using System.Text.RegularExpressions;
using System.Net;
using Uhuru.Openshift.Common.JsonHelper;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Uhuru.Openshift.Tests")]

namespace Uhuru.Openshift.Runtime
{
    public partial class ApplicationContainer
    {
        public const double PARALLEL_CONCURRENCY_RATIO = 0.2;
        public const int MAX_THREADS = 8;
        public const string RESULT_SUCCESS = "success";
        public const string RESULT_FAILURE = "failure";

        public string Uuid { get; set; }
        public string ApplicationUuid { get; set; }
        public string ContainerName { get; set; }
        public string ApplicationName { get; set; }
        public string Namespace { get; set; }
        public string BaseDir { get; set; }
        
        public int uid = 0;
        public int gid = 0;
        public string gecos = string.Empty;
        
        public object QuotaBlocks { get; set; }
        public object QuotaFiles { get; set; }

        public bool StopLock
        {
            get
            {
                return this.Cartridge.StopLockExists;
            }
        }

        public string ContainerDir 
        { 
            get 
            { 
                return Path.Combine(NodeConfig.Values["GEAR_BASE_DIR"], this.Uuid);
            } 
        }

        public GearRegistry GearRegist
        {
            get
            {
                if (this.gearRegistry == null && this.Cartridge.WebProxy() != null)
                {
                    this.gearRegistry = new GearRegistry(this);
                }
                return this.gearRegistry;
            }
        }
                
        public CartridgeModel Cartridge { get; set; }
        public Hourglass GetHourglass { get { return this.hourglass; } }
        public ApplicationState State { get; set; }

        ContainerPlugin containerPlugin;
        NodeConfig config;
        private Hourglass hourglass;
        private GearRegistry gearRegistry;


        public static ApplicationContainer GetFromUuid(string containerUuid, Hourglass hourglass = null)
        {
            EtcUser etcUser = GetPasswdFor(containerUuid);
            string nameSpace = null;
            Dictionary<string,string> env = Environ.Load(Path.Combine(LinuxFiles.Cygpath(etcUser.Dir, true), ".env"));
            
            if (!string.IsNullOrEmpty(env["OPENSHIFT_GEAR_DNS"]))
            {
                nameSpace = Regex.Replace(Regex.Replace("testing-uhu.openshift.local", @"\..*$", ""), @"^.*\-" ,"");
            }

            if (string.IsNullOrEmpty(env["OPENSHIFT_APP_UUID"]))
            {
                //Maybe we should improve the exceptions we throw.
                throw new Exception("OPENSHIFT_APP_UUID is missing!");
            }
            if (string.IsNullOrEmpty(env["OPENSHIFT_APP_NAME"]))
            {
                throw new Exception("OPENSHIFT_APP_NAME is missing!");
            }
            if (string.IsNullOrEmpty(env["OPENSHIFT_GEAR_NAME"]))
            {
                throw new Exception("OPENSHIFT_GEAR_NAME is missing!");
            }

            ApplicationContainer applicationContainer = new ApplicationContainer(env["OPENSHIFT_APP_UUID"], containerUuid, etcUser, 
                env["OPENSHIFT_APP_NAME"], env["OPENSHIFT_GEAR_NAME"], nameSpace, null, null, hourglass);

            return applicationContainer;

        }

        private static EtcUser GetPasswdFor(string containerUuid)
        {
            NodeConfig config = new Config.NodeConfig();

            string gecos = config.Get("GEAR_GECOS");

            if (string.IsNullOrEmpty(gecos))
            {
                gecos = "OO application container";
            }
            EtcUser etcUser = new Etc(config).GetPwanam(containerUuid);
            etcUser.Gecos = gecos;
            
            return etcUser;

        }

        public ApplicationContainer(string applicationUuid, string containerUuid, EtcUser userId, string applicationName,
            string containerName, string namespaceName, object quotaBlocks, object quotaFiles, Hourglass hourglass,int applicationUid=0)
        {
            this.config = NodeConfig.Values;
            this.Uuid = containerUuid;
            this.ApplicationUuid = applicationUuid;
            this.ApplicationName = applicationName;
            this.ContainerName = containerName;
            this.Namespace = namespaceName;
            this.QuotaBlocks = quotaBlocks;
            this.QuotaFiles = quotaFiles;
            this.State = new ApplicationState(this);            
            this.hourglass = hourglass ?? new Hourglass(3600);
            this.BaseDir = this.config["GEAR_BASE_DIR"];
            this.containerPlugin = new ContainerPlugin(this);
            this.Cartridge = new CartridgeModel(this, this.State, this.hourglass);            
            if (userId != null)
            {
                this.uid = userId.Uid;
                this.gid = userId.Gid;
                this.gecos = userId.Gecos;
            }
            if (applicationUid > 0)
            {
                this.uid = applicationUid;
            }
        }

        public string Create(string secretToken = null)
        {            
            containerPlugin.Create();
            return string.Empty;
        }

        public string Destroy()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(this.Cartridge.Destroy());

            this.Cartridge.EachCartridge(cart => output.AppendLine(this.DeletePublicEndpoints(cart.Name)));

            output.AppendLine(this.containerPlugin.Destroy());            
            return output.ToString();
        }

        public string KillProcs(dynamic options = null)
        {
            // TODO need to kill all user processes. stopping gear for now
            return this.StopGear(options);
        }

        public string Configure(string cartName, string templateGitUrl, string manifest, bool doExposePorts)        
        {
            string output = Cartridge.Configure(cartName, templateGitUrl, manifest);
            if (doExposePorts)
            {
               output = output + CreatePublicEndpoints(cartName);
            }
            return output;
        }

        public string ConnectorExecute(string cartName, string hookName, string publishingCartName, string connectionType, string inputArgs)
        {
            return Cartridge.ConnectorExecute(cartName, hookName, publishingCartName, connectionType, inputArgs);
        }

        public string Start(string cartName, dynamic options = null)
        {
            if (options == null)
            {
                options = new Dictionary<string,object>();
            }
            return this.Cartridge.StartCartridge("start", cartName, options);
        }

        public string Stop(string cartName, dynamic options = null)
        {
            if (options == null)
            {
                options = new Dictionary<string, object>();
            }
            return this.Cartridge.StopCartridge(cartName, true, options);
        }

        public string AddSshKey(string sshKey, string keyType, string comment)
        {
            string output = "";

            string key = string.Format("{0} {1} {2}", keyType, sshKey, comment);

            Sshd.AddKey(NodeConfig.Values["SSHD_BASE_DIR"], this.Uuid, key);       

            return output;
        }

        public string AddSshKeys(List<SshKey> sshKeys)
        {
            string output = "";

            foreach (SshKey sshKey in sshKeys)
            {
                AddSshKey(sshKey.Key, sshKey.Type, sshKey.Comment);
            }

            return output;
        }


        public void Distribute(dynamic options)
        {

        }

        private void PruneDeployments()
        {
            // TODO implement this!
        }



        public string StartGear(dynamic options)
        {
            return this.Cartridge.StartGear(options);
        }

        public void SetRWPermissions(string filename)
        {

        }

        public string StopGear(dynamic options)
        {
            return this.Cartridge.StopGear(options);
        }

        public List<RubyHash> Restart(string cartName, RubyHash options)
        {
            List<RubyHash> result = WithGearRotation(options,
                (GearRotationCallback)delegate(object targetGear, Dictionary<string, string> localGearEnv, RubyHash opts)
                {
                    return RestartGear(targetGear, localGearEnv, cartName, opts);
                });


            return result;
        }

        public void ReportDeployments(Dictionary<string, string> gearEnv)
        {
            string brokerAddr = NodeConfig.Values["BROKER_HOST"];
            string domain = gearEnv["OPENSHIFT_NAMESPACE"];
            string appName = gearEnv["OPENSHIFT_APP_NAME"];
            string appUuid = gearEnv["OPENSHIFT_APP_UUID"];
            string url = string.Format("https://{0}/broker/rest/domain/{1}/application/{2}/deployments", brokerAddr, domain, appName);
            Dictionary<string, string> param = BrokerAuthParams();
            if (param != null)
            {
                List<RubyHash> deployments = CalculateDeployments();
                param["deployments[]"] = JsonConvert.SerializeObject(deployments);
                param["applicaition_id"] = appUuid;
                string payload = JsonConvert.SerializeObject(param);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Headers.Add("accept", "application/json;version=1.6");
                request.Headers.Add("user_agent", "OpenShift");
                using (StreamWriter sw = new StreamWriter(request.GetRequestStream()))
                {
                    sw.Write(payload);
                    sw.Flush();
                    sw.Close();
                }
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if ((int)response.StatusCode >= 300)
                {
                    string result;
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        result = sr.ReadToEnd();
                    }
                    throw new Exception(result);
                }
            }
        }

        public void SetAutoDeploy(bool autoDeploy)
        {
            AddEnvVar("AUTO_DEPLOY", autoDeploy.ToString().ToLower(), true);
        }

        public void SetKeepDeployments(int keepDeployments)
        {
            AddEnvVar("KEEP_DEPLOYMENTS", keepDeployments.ToString(), true);
            // TODO Clean up any deployments over the limit
        }

        public void SetDeploymentBranch(string deploymentBranch)
        {
            AddEnvVar("DEPLOYMENT_BRANCH", deploymentBranch, true);
        }

        public void SetDeploymentType(string deploymentType)
        {
            AddEnvVar("DEPLOYMENT_TYPE", deploymentType, true);
        }

        public delegate dynamic GearRotationCallback(object targetGear, Dictionary<string, string> localGearEnv, RubyHash options);
        public List<RubyHash> WithGearRotation(RubyHash options, GearRotationCallback action)
        {
            dynamic localGearEnv = Environ.ForGear(this.ContainerDir);
            Manifest proxyCart = this.Cartridge.WebProxy();
            List<object> gears = new List<object>();

            // TODO: vladi: verify if this is needed for scalable apps
            //if (options.ContainsKey("all") && proxyCart != null)
            //{
            //    if ((bool)options["all"])
            //    {
            //        gears = this.GearRegist.Entries["web"].Keys.ToList<object>();
            //    }
            //    else if (options.ContainsKey("gears"))
            //    {
            //        List<string> g = (List<string>)options["gears"];
            //        gears = this.GearRegist.Entries["web"].Keys.Where(e => g.Contains(e)).ToList<object>();
            //    }
            //    else
            //    {
            //        try
            //        {
            //            gears.Add(this.GearRegist.Entries["web"][this.Uuid]);
            //        }
            //        catch
            //        {
            //            gears.Add(this.Uuid);
            //        }
            //    }
            //}
            //else
            {
                gears.Add(this.Uuid);
            }

            double parallelConcurrentRatio = PARALLEL_CONCURRENCY_RATIO;
            if (options.ContainsKey("parallel_concurrency_ratio"))
            {
                parallelConcurrentRatio = (double)options["parallel_concurrency_ratio"];
            }

            int batchSize = CalculateBatchSize(gears.Count, parallelConcurrentRatio);

            int threads = Math.Max(batchSize, MAX_THREADS);

            List<RubyHash> result = new List<RubyHash>();

            // need to parallelize
            foreach (var targetGear in gears)
            {
                result.Add(RotateAndYield(targetGear, localGearEnv, options, action));
            }

            return result;
        }

        public RubyHash RotateAndYield(object targetGear, Dictionary<string, string> localGearEnv, RubyHash options, GearRotationCallback action)
        {
            RubyHash result = new RubyHash()
            {
                { "status", RESULT_FAILURE },
                { "messages", new List<string>() },
                { "errors", new List<string>() }
            };

            string proxyCart = options["proxy_cart"];

            string targetGearUuid = targetGear is string ? (string)targetGear : ((GearRegistry.Entry)targetGear).Uuid;

            // TODO: vladi: check if this condition also needs boolean verification on the value in the hash
            if (options["init"] == null && options["rotate"] != null && options["rotate"] && options["hot_deploy"] != null && options["hot_deploy"])
            {
                result["messages"].Add("Rotating out gear in proxies");

                RubyHash rotateOutResults = this.UpdateProxyStatus(new RubyHash()
                    {
                        { "action", "disable" },
                        { "gear_uuid", targetGearUuid },
                        { "cartridge", proxyCart }
                    });

                result["rotate_out_results"] = rotateOutResults;

                if (rotateOutResults["status"] != RESULT_SUCCESS)
                {
                    result["errors"].Add("Rotating out gear in proxies failed.");
                    return result;
                }
            }

            RubyHash yieldResult = action(targetGear, localGearEnv, options);

            dynamic yieldStatus = yieldResult.Delete("status");
            dynamic yieldMessages = yieldResult.Delete("messages");
            dynamic yieldErrors = yieldResult.Delete("errors");

            result["messages"].AddRange(yieldMessages);
            result["errors"].AddRange(yieldErrors);

            result = result.Merge(yieldResult);

            if (yieldStatus != RESULT_SUCCESS)
            {
                return result;
            }

            if (options["init"] == null && options["rotate"] != null && options["rotate"] && options["hot_deploy"] != null && options["hot_deploy"])
            {
                result["messages"].Add("Rotating in gear in proxies");

                RubyHash rotateInResults = this.UpdateProxyStatus(new RubyHash()
                {
                    { "action", "enable" },
                    { "gear_uuid", targetGearUuid },
                    { "cartridge", proxyCart }
                });

                result["rotate_in_results"] = rotateInResults;

                if (rotateInResults["status"] != RESULT_SUCCESS)
                {
                    result["errors"].Add("Rotating in gear in proxies failed");
                    return result;
                }
            }

            result["status"] = RESULT_SUCCESS;

            return result;
        }

        public RubyHash UpdateProxyStatus(RubyHash options)
        {
            string action = options["action"];

            if (action != "enable" && action != "disable")
            {
                throw new ArgumentException("action must either be :enable or :disable");
            }

            if (options["gear_uuid"] == null)
            {
                throw new ArgumentException("gear_uuid is required");
            }

            string gearUuid = options["gear_uuid"];

            Manifest cartridge;

            if (options["cartridge"] == null)
            {
                cartridge = this.Cartridge.WebProxy();
            }
            else
            {
                cartridge = options["cartridge"];
            }

            if (cartridge == null)
            {
                throw new ArgumentException("Unable to update proxy status - no proxy cartridge found");
            }

            dynamic persist = options["persist"];

            Dictionary<string, string> gearEnv = Environ.ForGear(this.ContainerDir);

            RubyHash result = new RubyHash() {
                { "status", RESULT_SUCCESS },
                { "target_gear_uuid", gearUuid },
                { "proxy_results", new RubyHash() }
            };

            RubyHash gearResult = new RubyHash();

            if (gearEnv["OPENSHIFT_APP_DNS"] != gearEnv["OPENSHIFT_GEAR_DNS"])
            {
                gearResult = this.UpdateLocalProxyStatus(new RubyHash(){
                    { "cartridge", cartridge },
                    { "action", action },
                    { "proxy_gear", this.Uuid },
                    { "target_gear", gearUuid },
                    { "persist", persist }
                });

                result["proxy_results"][this.Uuid] = gearResult;
            }
            else
            {
                // only update the other proxies if we're the currently elected proxy
                // TODO the way we determine this needs to change so gears other than
                // the initial proxy gear can be elected
                GearRegistry.Entry[] proxyEntries = this.gearRegistry.Entries["proxy"].Values.ToArray();

                // TODO: vladi: Make this parallel
                RubyHash[] parallelResults = proxyEntries.Select(entry =>
                    this.UpdateRemoteProxyStatus(new RubyHash()
                    {
                        { "current_gear", this.Uuid },
                        { "proxy_gear", entry },
                        { "target_gear", gearUuid },
                        { "cartridge", cartridge },
                        { "action", action },
                        { "persist", persist },
                        { "gear_env", gearEnv }
                    })).ToArray();
                
                foreach (RubyHash parallelResult in parallelResults)
                {
                    if (parallelResult.ContainsKey("proxy_results"))
                    {
                        result["proxy_results"] = result["proxy_results"].Merge(parallelResult["proxy_results"]);
                    }
                    else
                    {
                        result["proxy_results"][parallelResult["proxy_gear_uuid"]] = parallelResult;
                    }
                }
            }

            // if any results failed, consider the overall operation a failure
            foreach (RubyHash proxyResult in result["proxy_results"].Values)
            {
                if (proxyResult["status"] != RESULT_SUCCESS)
                {
                    result["status"] = RESULT_FAILURE;
                }
            }

            return result;
        }

        public RubyHash UpdateLocalProxyStatus(RubyHash args)
        {
            RubyHash result = new RubyHash();

            object cartridge = args["cartridge"];
            object action = args["action"];
            object targetGear =args["target_gear"];
            object persist = args["persist"];

            try
            {
                string output = this.UpdateProxyStatusForGear(new RubyHash(){
                    { "cartridge", cartridge },
                    { "action", action },
                    { "gear_uuid", targetGear },
                    { "persist", persist }
                });

                result = new RubyHash()
                {
                    { "status", RESULT_SUCCESS },
                    { "proxy_gear_uuid", this.Uuid },
                    { "target_gear_uuid", targetGear },
                    { "messages", new List<string>() },
                    { "errors", new List<string>() }
                };
            }
            catch (Exception ex)
            {
                result = new RubyHash()
                {
                    { "status", RESULT_FAILURE },
                    { "proxy_gear_uuid", this.Uuid },
                    { "target_gear_uuid", targetGear },
                    { "messages", new List<string>() },
                    { "errors", new List<string>() { string.Format("An exception occured updating the proxy status: {0}\n{1}", ex.Message, ex.StackTrace) } }
                };
            }

            return result;
        }

        public string UpdateProxyStatusForGear(RubyHash options)
        {
            string action = options["action"];

            if (action != "enable" && action != "disable")
            {
                new ArgumentException("action must either be :enable or :disable");
            }

            string gearUuid = options["gear_uuid"];

            if (gearUuid == null)
            {
                new ArgumentException("gear_uuid is required");
            }

            Manifest cartridge = options["cartridge"] ?? this.Cartridge.WebProxy();

            if (cartridge == null)
            {
                throw new ArgumentNullException("Unable to update proxy status - no proxy cartridge found");
            }

            bool persist = options["persist"] != null && options["persist"] == true;
            string control = string.Format("{0}-server", action);

            List<string> args = new List<string>();

            if (persist)
            {
                args.Add("persist");
            }

            args.Add(gearUuid);

            return this.Cartridge.DoControl(
                control, cartridge, new Dictionary<string, object>()
                {
                    { "args", string.Join(" ", args) },
                    { "pre_action_hooks_enabled", false },
                    { "post_action_hooks_enabled", false }
                });
        }

        public RubyHash UpdateRemoteProxyStatus(RubyHash args)
        {
            RubyHash result = new RubyHash();
            string currentGear = args["current_gear"];
            GearRegistry.Entry proxyGear = args["proxy_gear"];
            object targetGear = args["target_gear"];
            Manifest cartridge = args["cartridge"];
            string action = args["action"];
            bool persist = args["persist"] != null && args["persist"] == true;
            object gearEnv = args["gear_env"];

            if (currentGear == proxyGear.Uuid)
            {
                // self, no need to ssh
                return this.UpdateLocalProxyStatus(new RubyHash()
                {
                    { "cartridge", cartridge }, 
                    { "action", action }, 
                    { "target_gear", targetGear }, 
                    { "persist", persist }
                });
            }

            string direction = action == "enable" ? "in" : "out";
            string persistOption = persist ? "--persist" : "";

            string url = string.Format("{0}@{1}", proxyGear.Uuid, proxyGear.ProxyHostname);

            string ooSSH = @"/cygpath/c/openshift/oo-bin/oo-ssh";
            string bashBinary = Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], "bin\bash.exe");

            string sshCommand = string.Format("{0} {1} gear rotate-{2} --gear {3} {4} --cart {5}-{6} --as-json", 
                ooSSH, url, direction, targetGear, persistOption, cartridge.Name, cartridge.Version);

            string bashArgs = string.Format("--norc --login -c '{0}'", sshCommand);

            string command = string.Format("{0} {1}", bashBinary, bashArgs);

            try
            {
                ProcessResult processResult = this.RunProcessInContainerContext(this.ContainerDir, command, 0);

                if (string.IsNullOrEmpty(processResult.StdOut))
                {
                    throw new Exception("No result JSON was received from the remote proxy update call");
                }

                result = JsonConvert.DeserializeObject<RubyHash>(processResult.StdOut);

                if (!result.ContainsKey("status"))
                {
                    throw new Exception(string.Format("Invalid result JSON received from remote proxy update call: {0}", processResult.StdOut));
                }
            }
            catch (Exception ex)
            {
                result = new RubyHash()
                {
                    { "status", RESULT_FAILURE },
                    { "proxy_gear_uuid", proxyGear.Uuid },
                    { "messages", new List<string>() },
                    { "errors", new List<string> { string.Format("An exception occured updating the proxy status: {0}\n{1}", ex.Message, ex.StackTrace) } }
                };
            }

            return result;
        }

        public RubyHash RestartGear(object targetGear, Dictionary<string, string> localGearEnv, string cartName, dynamic options)
        {
            string targetGearUuid = targetGear is string ? (string)targetGear : ((GearRegistry.Entry)targetGear).Uuid;
            RubyHash result = new RubyHash()
            {
                { "status", RESULT_SUCCESS },
                { "messages", new List<string>() },
                { "errors", new List<string>() },
                { "target_gear_uuid", targetGearUuid }
            };

            try
            {
                if (targetGearUuid == this.Uuid)
                {
                    result["messages"].Add(this.Cartridge.StartCartridge("restart", cartName, options));
                }
                else
                {

                    string ooSSH = @"/cygpath/c/openshift/oo-bin/oo-ssh";
                    string bashBinary = Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], "bin\bash.exe");

                    if (targetGear is string)
                    {
                        targetGear = new GearRegistry.Entry(options);
                    }
                    string sshCommand = string.Format("{0} {1} gear restart --cart {2} --as-json",
                        ooSSH, ((GearRegistry.Entry)targetGear).ToSshUrl(), cartName);
                    string bashArgs = string.Format("--norc --login -c '{0}'", sshCommand);
                    string command = string.Format("{0} {1}", bashBinary, bashArgs);

                    RunProcessInContainerContext(this.ContainerDir, command);

                }
            }
            catch(Exception ex)
            {
                result["errors"].Add(ex.ToString());
                result["status"] = RESULT_FAILURE;
                Logger.Error(ex.ToString());
            }

            return result;
        }

        public string Reload(string cartName)
        {
            if (Runtime.State.STARTED.EqualsString(this.State.Value()))
            {
                return this.Cartridge.DoControl("reload", cartName);
            }
            else
            {
                return this.Cartridge.DoControl("force-reload", cartName);
            }
        }

        public ProcessResult RunProcessInContainerContext(string gearDirectory, string cmd, int expectedExitStatus = -1, int timeout = 3600000)
        {
            StringBuilder output = new StringBuilder();
            StringBuilder outputErr = new StringBuilder();

            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH") + ";" + Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], "bin");
            processStartInfo.EnvironmentVariables["HOME"] = this.ContainerDir;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.WorkingDirectory = gearDirectory;

            processStartInfo.FileName = Path.Combine(Path.GetDirectoryName(typeof(ApplicationContainer).Assembly.Location), "oo-trap-user.exe");
            processStartInfo.Arguments = "-c \"" + cmd.Replace('\\','/') + "\"";
            
            Process process = new Process();
            process.StartInfo = processStartInfo;

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        outputErr.AppendLine(e.Data);
                    }
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (process.WaitForExit(timeout) &&
                    outputWaitHandle.WaitOne(timeout) &&
                    errorWaitHandle.WaitOne(timeout))
                {
                    Logger.Debug("Shell command '{0}' ran. rc={1} out={2} err={3}", cmd, process.ExitCode, output, outputErr);

                    if (expectedExitStatus != -1 && process.ExitCode != expectedExitStatus)
                    {
                        Logger.Warning("Shell command '{0}' returned an error. rc={1}", cmd, process.ExitCode);
                        throw new Exception(string.Format("Shell command '{0}' returned an error. rc={1}", cmd, process.ExitCode));
                    }
                }
                else
                {
                    // Timed out. Kill process tree.
                    process.KillProcessAndChildren();
                    Logger.Warning("Shell command '{0}' exceeded timeout of {1}", cmd, timeout / 1000);
                    throw new Exception(string.Format("Shell command '{0}' exceeded timeout of {1}", cmd, timeout / 1000));
                }
            }

            return new ProcessResult()
            {
                ExitCode = process.ExitCode,
                StdOut = output.ToString(),
                StdErr = outputErr.ToString()
            };
        }

        internal void SetRoPermissions(string hooks)
        {
        }

        internal void InitializeHomedir(string baseDir, string homeDir)
        {
            Directory.CreateDirectory(Path.Combine(homeDir, ".tmp"));
            Directory.CreateDirectory(Path.Combine(homeDir, ".sandbox"));
            
            string sandboxUuidDir = Path.Combine(homeDir, ".sandbox", this.Uuid);
            Directory.CreateDirectory(sandboxUuidDir);
            SetRWPermissions(sandboxUuidDir);

            string envDir = Path.Combine(homeDir, ".env");
            Directory.CreateDirectory(envDir);
            SetRoPermissions(envDir);

            string userEnvDir = Path.Combine(homeDir, ".env", "user_vars");
            Directory.CreateDirectory(userEnvDir);
            SetRoPermissions(userEnvDir);

            string sshDir = Path.Combine(homeDir, ".ssh");
            Directory.CreateDirectory(sshDir);
            SetRoPermissions(sshDir);

            string gearDir = Path.Combine(homeDir, this.ContainerName);
            string gearAppDir = Path.Combine(homeDir, "app-root");

            AddEnvVar("APP_DNS", string.Format("{0}-{1}.{2}", this.ApplicationName, this.Namespace, this.config["CLOUD_DOMAIN"]), true);
            AddEnvVar("APP_NAME", this.ApplicationName, true);
            AddEnvVar("APP_UUID", this.ApplicationUuid, true);
            
            string dataDir = Path.Combine(gearAppDir, "data");
            Directory.CreateDirectory(dataDir);
            AddEnvVar("DATA_DIR", dataDir, true);

            string deploymentsDir = Path.Combine(homeDir, "app-deployments");
            Directory.CreateDirectory(deploymentsDir);
            AddEnvVar("DEPLOYMENTS_DIR", deploymentsDir, true);
            Directory.CreateDirectory(Path.Combine(deploymentsDir, "by-id"));

            CreateDeploymentDir();

            AddEnvVar("GEAR_DNS", string.Format("{0}-{1}.{2}", this.ContainerName, this.Namespace, this.config["CLOUD_DOMAIN"]), true);
            AddEnvVar("GEAR_NAME", this.ContainerName, true);
            AddEnvVar("GEAR_UUID", this.Uuid, true);
            AddEnvVar("HOMEDIR", homeDir, true);
            AddEnvVar("HOME", homeDir, false);

            AddEnvVar("DEPENDENCIES_DIR", Path.Combine(gearAppDir, "runtime", "dependencies"), true);
            Directory.CreateDirectory(Path.Combine(gearAppDir, "runtime", "dependencies"));

            AddEnvVar("BUILD_DEPENDENCIES_DIR", Path.Combine(gearAppDir, "runtime", "build-dependencies"), true);
            Directory.CreateDirectory(Path.Combine(gearAppDir, "runtime", "build-dependencies"));

            AddEnvVar("NAMESPACE", this.Namespace, true);

            string repoDir = Path.Combine(gearAppDir, "runtime", "repo");
            AddEnvVar("REPO_DIR", repoDir, true);
            Directory.CreateDirectory(repoDir);

            this.State.Value(Runtime.State.NEW);
        }

        public void AddEnvVar(string key, string value, bool prefixCloudName)
        {
            string envDir = Path.Combine(this.ContainerDir, ".env");
            if (prefixCloudName)
            {
                key = string.Format("OPENSHIFT_{0}", key);
            }
            string fileName = Path.Combine(envDir, key);
            File.WriteAllText(fileName, value);
            SetRoPermissions(fileName);
        }

        public void AddEnvVar(string key, string value)
        {
            AddEnvVar(key, value, false);
        }

        public string ReadEnvVar(string key)
        {
            return ReadEnvVar(key, false);
        }

        public string ReadEnvVar(string key, bool prefixCloudName)
        {
            string envDir = Path.Combine(this.ContainerDir, ".env");
            if (prefixCloudName)
            {
                key = string.Format("OPENSHIFT_{0}", key);
            }
            string fileName = Path.Combine(envDir, key);
            if (File.Exists(fileName))
            {
                return File.ReadAllText(fileName);
            }
            else
            {
                return null;
            }
        }

        public string ForceStop(Dictionary<string, object> options = null)
        {
            this.State.Value(Uhuru.Openshift.Runtime.State.STOPPED);
            return this.containerPlugin.Stop();
        }

        public Manifest GetCartridge(string cartridgeName)
        {

            return Cartridge.GetCartridge(cartridgeName);
        }

        private int CalculateBatchSize(int count, double ratio)
        {
            return (int)(Math.Max(1 / ratio, count) * ratio);
        }

        public string Tidy()
        {
            StringBuilder output = new StringBuilder();

            Dictionary<string, string> env = Environ.ForGear(this.ContainerDir);
            
            string gearDir = env["OPENSHIFT_HOMEDIR"];
            string appName = env["OPENSHIFT_APP_NAME"];

            string gearRepoDir = Path.Combine(gearDir, "git", string.Format("{0}.git", appName));
            string gearTmpDir = Path.Combine(gearDir, ".tmp");

            output.Append(StopGear(new Dictionary<string, object>() { { "user_initiated", false } }));
            try
            {
                GearLevelTidyTmp(gearTmpDir);
                output.AppendLine(this.Cartridge.Tidy());
                output.AppendLine(GearLevelTidyGit(gearRepoDir));
            }
            catch (Exception ex)
            {
                output.AppendLine(ex.ToString());
            }
            finally
            {
                StartGear(new Dictionary<string, object>() { { "user_initiated", false } });
            }
            return output.ToString();
        }

        private void GearLevelTidyTmp(string gearTmpDir)
        {
            DirectoryUtil.EmptyDirectory(gearTmpDir);
        }

        private string GearLevelTidyGit(string gearRepoDir)
        {
            StringBuilder output = new StringBuilder();
            string gitPath = Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], @"bin\git.exe");
            string cmd = string.Format("{0} prune", gitPath);
            output.AppendLine(RunProcessInContainerContext(gearRepoDir, cmd).StdOut);

            cmd = string.Format("{0} gc --aggressive", gitPath);
            output.AppendLine(RunProcessInContainerContext(gearRepoDir, cmd).StdOut);
            return output.ToString();
        }

        private string StoppedStatusAttr()
        {
            if (Runtime.State.STOPPED.EqualsString(this.State.Value()) || this.Cartridge.StopLockExists)
            {
                return "ATTR: status=ALREADY_STOPPED" + Environment.NewLine;
            }
            else
            {
                if (Runtime.State.IDLE.EqualsString(this.State.Value()))
                {
                    return "ATTR: status=ALREADY_IDLED" + Environment.NewLine;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        protected Dictionary<string, string> BrokerAuthParams()
        {
            string authToken = Path.Combine(NodeConfig.Values["GEAR_BASE_DIR"], this.Uuid, ".auth", "token");
            string authIv = Path.Combine(NodeConfig.Values["GEAR_BASE_DIR"], this.Uuid, ".auth", "iv");
            if (File.Exists(authToken) && File.Exists(authIv))
            {
                Dictionary<string, string> param = new Dictionary<string, string>();
                param["broker_auth_key"] = File.ReadAllText(authToken);
                param["broker_auth_iv"] = File.ReadAllText(authIv);
                return param;
            }
            else
            {
                return null;
            }
        }

        internal List<string> BuildDependencyDirs(Manifest cartridge)
        {
            throw new NotImplementedException();
        }


        public static IEnumerable<ApplicationContainer> All(Hourglass hourglass = null, bool loadenv = true)
        {
            EtcUser[] users = new Etc(NodeConfig.Values).GetAllUsers();

            foreach (EtcUser user in users)
            {
                if (user.Gecos.StartsWith("openshift_service"))
                {
                    RubyHash env = new RubyHash();
                    string gearNamespace = null;

                    if (loadenv)
                    {
                        env = new RubyHash(Environ.Load(new string[] { Path.Combine(user.Dir, ".env") }));
                    }

                    if (env.ContainsKey("OPENSHIFT_GEAR_DNS"))
                    {
                        gearNamespace = env["OPENSHIFT_GEAR_DNS"];
                    }

                    ApplicationContainer app = null;
                    
                    try
                    {
                        app = new ApplicationContainer(env["OPENSHIFT_APP_UUID"],
                            user.Name, user, env["OPENSHIFT_APP_NAME"], env["OPENSHIFT_GEAR_NAME"], gearNamespace, null, null, hourglass);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to instantiate ApplicationContainer for uid {0}/uuid {1}: {2}",
                            user.Uid, env["OPENSHIFT_APP_UUID"], ex.Message);
                        Logger.Error("Stacktrace: {0}", ex.StackTrace);

                        continue;
                    }

                    yield return app;
                }
            }
        }
    }
}
