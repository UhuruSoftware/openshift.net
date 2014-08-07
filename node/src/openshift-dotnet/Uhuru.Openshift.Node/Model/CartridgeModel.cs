using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Uhuru.Openshift.Common.Models;
using Uhuru.Openshift.Common.Utils;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Model;
using Uhuru.Openshift.Runtime.Utils;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Runtime
{
    public class CartridgeModel
    {
        public string StopLock
        {
            get
            {
                return Path.Combine(this.container.ContainerDir, "app-root", "runtime", ".stop_lock");
            }
        }

        public bool StopLockExists
        {
            get
            {
                return File.Exists(this.StopLock);
            }
        }

        private ApplicationContainer container;
        private ApplicationState state;
        private Hourglass hourglass;
        private int timeout;
        private Dictionary<string, Manifest> cartridges;        

        public CartridgeModel(ApplicationContainer container, ApplicationState state, Hourglass hourglass)
        {
            this.container = container;
            this.state = state;
            this.hourglass = hourglass;
            this.timeout = 30;
            this.cartridges = new Dictionary<string, Manifest>();
        }

        public string Configure(string cartName, string templateGitUrl, string manifest)
        {
            StringBuilder sb = new StringBuilder();

            this.CartridgeName = cartName;            
            string name = cartName.Split('-')[0];
            string softwareVersion = cartName.Split('-')[1];
            Manifest cartridge = null;
            if (!string.IsNullOrEmpty(manifest))
            {
                Logger.Debug("Loading from manifest... {0}", manifest);
                cartridge = new Manifest(manifest, softwareVersion);
            }
            else
            {
                cartridge = CartridgeRepository.Instance.Select(name, softwareVersion);
            }
           
            CreateCartridgeDirectory(cartridge, softwareVersion);
            this.CreatePrivateEndpoints(cartridge);
            this.CreateDependencyDirectories(cartridge);
            sb.AppendLine(CartridgeAction(cartridge, "setup", softwareVersion, true));
            sb.AppendLine(CartridgeAction(cartridge, "install", softwareVersion));
            sb.AppendLine(PopulateGearRepo(name, softwareVersion, templateGitUrl));
            return sb.ToString();
        }

        public Manifest GetPrimaryCartridge()
        {
            Dictionary<string,string> env = Environ.ForGear(container.ContainerDir);
            string primaryCartDir = null;
            if (env.ContainsKey("OPENSHIFT_PRIMARY_CARTRIDGE_DIR"))
            {
                primaryCartDir = env["OPENSHIFT_PRIMARY_CARTRIDGE_DIR"];
            }
            else
            {
                return null;
            }

            return GetCartridgeFromDirectory(primaryCartDir);
        }


        public string StopGear(dynamic options)
        {
            StringBuilder output = new StringBuilder();
            EachCartridge(delegate(Manifest cartridge)
            {
                output.AppendLine(StopCartridge(cartridge, options));
            });
            return output.ToString();
        }

        public Manifest WebProxy()
        {
            Manifest result = null;
            EachCartridge(delegate(Manifest cartridge)
            {
                if (cartridge.WebProxy)
                {
                    result = cartridge;
                }
            });
            return result;
        }

        public delegate void ProcessCartridgeCallback(string cartDir);
        public delegate void EachCartridgeCallback(Manifest cartridge);

        public void EachCartridge(EachCartridgeCallback action)
        {
            ProcessCartridges(null,
                delegate(string cartridgeDir)
                {
                    Manifest cartridge = GetCartridgeFromDirectory(cartridgeDir);
                    action(cartridge);
                });
        }

        /// <summary>
        /// Let a cart perform some action when another cart is being removed
        /// Today, it is used to cleanup environment variables
        /// </summary>
        /// <param name="cartName">Unsubscribing cartridge name.</param>
        /// <param name="pubCartName">Publishing cartridge name.</param>
        public void Unsubscribe(string cartName, string pubCartName)
        {
            string envDirPath = Path.Combine(container.ContainerDir, ".env", ShortNameFromFullCartName(pubCartName));
            Directory.Delete(envDirPath, true);
        }

        public string Destroy()
        {
            StringBuilder output = new StringBuilder();
            EachCartridge(delegate(Manifest cartridge)
                {
                    output.AppendLine(CartridgeTeardown(cartridge.Dir, false));
                });
            return output.ToString();
        }

        private string CartridgeTeardown(string cartridgeName, bool removeCartridgeDir)
        {
            string cartridgeHome = Path.Combine(this.container.ContainerDir, cartridgeName);
            Dictionary<string, string> env = Environ.ForGear(this.container.ContainerDir, cartridgeHome);
            string teardown = Path.Combine(cartridgeHome, "bin", "teardown.ps1");
            if (!File.Exists(teardown))
            {
                return string.Empty;
            }

            // run teardown script

            return string.Empty;
        }

        private void ProcessCartridges(string cartridgeDir, ProcessCartridgeCallback action)
        {
            if (!string.IsNullOrEmpty(cartridgeDir))
            {
                string cartDir = Path.Combine(this.container.ContainerDir, cartridgeDir);
                action(cartDir);
                return;
            }
            else
            {
                foreach (string dir in Directory.GetDirectories(container.ContainerDir))
                {
                    if(File.Exists(Path.Combine(dir, "metadata", "manifest.yml")))
                    {
                        action(dir);
                    }
                }
            }
        }

        public string StartGear(dynamic options)
        {
            StringBuilder output = new StringBuilder();
            EachCartridge(delegate(Manifest cartridge)
            {
                output.AppendLine(StartCartridge("start", cartridge, options));
            });
            return output.ToString();
        }

        public string StopCartridge(string cartridgeName, bool userInitiated, dynamic options)
        {

            Manifest manifest = GetCartridge(cartridgeName);
            if (!options.ContainsKey("user_initiated"))
            {
                options.Add("user_initiated", userInitiated);
            }
            else
            {
                options["user_initiated"] = userInitiated;
            }
            return StopCartridge(manifest, options);

        }

        public string StopCartridge(Manifest cartridge, dynamic options)
        {
            Logger.Debug("Stopping cartridge {0} for gear {1}", cartridge.Name, this.container.Uuid);

            if (options == null)
            {
                options = new Dictionary<string, object>();
            }
            options = (Dictionary<string, object>)options;
            if (!options.ContainsKey("user_initiated"))
            {
                options.Add("user_initiated", true);
            }

            if (!options.ContainsKey("hot_deploy"))
            {
                options.Add("hot_deploy", false);
            }

            if (options["hot_deploy"])
            {
                return string.Format("Not stopping cartridge {0} because hot deploy is enabled", cartridge.Name);
            }

            if (!options["user_initiated"] && StopLockExists)
            {
                return string.Format("Not stopping cartridge {0} because the application was explicitly stopped by the user", cartridge.Name);
            }

            Manifest primaryCartridge = GetPrimaryCartridge();
            if (primaryCartridge != null)
            {
                if (cartridge.Name == primaryCartridge.Name)
                {
                    if (options["user_initiated"])
                    {
                        CreateStopLock();
                    }
                    state.Value(State.STOPPED);
                }
            }

            return DoControl("stop", cartridge, options);
        }
        
        public string StartCartridge(string action, Manifest cartridge, dynamic options)
        {
            options = (Dictionary<string, object>)options;
            if (!options.ContainsKey("user_initiated"))
            {
                options.Add("user_initiated", true);
            }

            if (!options.ContainsKey("hot_deploy"))
            {
                options.Add("hot_deploy", false);
            }

            if (!options["user_initiated"] && StopLockExists)
            {
                return string.Format("Not starting cartridge {0} because the application was explicitly stopped by the user", cartridge.Name);
            }

            Manifest primaryCartridge = GetPrimaryCartridge();            

            if (primaryCartridge != null)
            {
                if (primaryCartridge.Name == cartridge.Name)
                {
                    if (options["user_initiated"])
                    {
                        File.Delete(StopLock);
                    }
                    state.Value(State.STARTED);

                //TODO : Unidle the application
                }
            }

            if (options["hot_deploy"])
            {
                return string.Format("Not starting cartridge {0} because hot deploy is enabled", cartridge.Name);
            }

            return DoControl(action, cartridge, options);
        }

        public string StartCartridge(string action, string cartridgeName, dynamic options)
        {
            Manifest manifest = GetCartridge(cartridgeName);
            return StartCartridge(action, manifest, options);

        }

        public string DoControl(string action, string cartridgeName, dynamic options = null)
        {
            Manifest manifest = GetCartridge(cartridgeName);
            return DoControl(action, manifest, options);
        }

        public string DoControl(string action, Manifest cartridge, dynamic options = null)
        {
            if (options == null)
            {
                options = new Dictionary<string, string>();
            }
            options["cartridgeDir"] = cartridge.Dir;
            return DoControlWithDirectory(action, options);
        }

        public string DoControlWithDirectory(string action, dynamic options)
        {
            // TODO: vladi: complete implementation of this method

            StringBuilder output = new StringBuilder();
            string cartridgeDirectory = options["cartridgeDir"];
            
            ProcessCartridges(cartridgeDirectory, delegate(string cartridgeDir)
            {
                bool isExe = File.Exists(Path.Combine(cartridgeDir, "bin", "control.exe"));
                string control = string.Empty;
                string cmd = string.Empty;

                if (isExe)
                {
                    control = Path.Combine(cartridgeDir, "bin", "control.exe");
                    cmd = string.Format("{0} {1}", control, action);
                }
                else
                {
                    control = Path.Combine(cartridgeDir, "bin", "control.ps1");
                    cmd = string.Format("{0} -ExecutionPolicy Bypass -InputFormat None -noninteractive -file {1} -command {2}", ProcessExtensions.Get64BitPowershell(), control, action);
                }
                ProcessResult processResult = container.RunProcessInContainerContext(container.ContainerDir, cmd);

                output.AppendLine(processResult.StdOut);
                output.AppendLine(processResult.StdErr);

                if(processResult.ExitCode != 0)
                {
                    throw new Exception(string.Format("CLIENT_ERROR: Failed to execute: 'control {0}' for {1}", action, container.ContainerDir));
                }
            });

            return output.ToString();
        }

        private string PopulateGearRepo(string cartName, string softwareVersion, string templateGitUrl)
        {
            ApplicationRepository repo = new ApplicationRepository(this.container);
            if (string.IsNullOrEmpty(templateGitUrl))
            {
                repo.PopulateFromCartridge(cartName);
            }
            else
            {
                repo.PopulateFromUrl(cartName, templateGitUrl);
            }
            if (repo.Exists())
            {
                repo.Archive(Path.Combine(this.container.ContainerDir, "app-root", "runtime", "repo"), "master");
            }

            var prison = Prison.Prison.LoadPrisonNoAttach(PrisonIdConverter.Generate(this.container.Uuid));

            // TODO (vladi): make sure there isn't a more elegant way to deal with SQL Server Instances
            if (cartName == "mssql" && softwareVersion == "2008")
            {
                Uhuru.Prison.MsSqlInstanceTool.ConfigureMsSqlInstanceRegistry(prison, "MSSQL10_50", "MSSQLSERVER");
                CreateSQLServerInstanceDatabases(cartName, prison, "MSSQL10_50", "MSSQLSERVER");
                ChangeConfigControl(cartName, softwareVersion, "MSSQL10_50");
            }
            else if (cartName == "mssql" && softwareVersion == "2012")
            {
                Uhuru.Prison.MsSqlInstanceTool.ConfigureMsSqlInstanceRegistry(prison, "MSSQL11", "MSSQLSERVER2012");
                CreateSQLServerInstanceDatabases(cartName, prison, "MSSQL11", "MSSQLSERVER2012");
                ChangeConfigControl(cartName, softwareVersion, "MSSQL11");
            }

            Logger.Debug("Setting permisions to home dir gear {0}, prison user {1}", this.container.Uuid, prison.User.Username);
            LinuxFiles.TakeOwnershipOfGearHome(this.container.ContainerDir, prison.User.Username);

            string gitDir = Path.Combine(this.container.ContainerDir, "git", "template", ".git");
            Logger.Debug("Setting permisions to git dir {0}, prison user {1}", gitDir, prison.User.Username);
            if (Directory.Exists(gitDir))
            {
                LinuxFiles.TakeOwnership(gitDir, prison.User.Username);
            }

            Logger.Debug("Setting permisions to git dir {0}, prison user {1}", repo.RepositoryPath, prison.User.Username);
            LinuxFiles.TakeOwnership(repo.RepositoryPath, prison.User.Username);

            return string.Empty;
        }

        public void CreateSQLServerInstanceDatabases(string cartName, Prison.Prison prison, string instanceType, string defaultInstanceName)
        {
            // TODO: vladi: GLOBAL LOCK
            Logger.Debug("Setting up SQL Server system databases for gear {0}, cart {1}", this.container.Uuid, cartName);
            
            string binLocation = Path.GetDirectoryName(this.GetType().Assembly.Location);
            string dbBuilderExe = Path.Combine(binLocation, "MsSQLSysGenerator.exe");

            string destination = Path.Combine(this.container.ContainerDir, cartName, "bin", string.Format("{0}.Instance{1}", instanceType, prison.Rules.UrlPortAccess), "mssql");
            string newSAPassword = string.Format("Pr!5{0}", Prison.Utilities.Credentials.GenerateCredential(10));

            string arguments = string.Format("dir={0} newPass={1} instanceType={2} defaultInstanceName={3}", destination, newSAPassword, instanceType, defaultInstanceName);

            ProcessResult result = ProcessExtensions.RunCommandAndGetOutput(dbBuilderExe, arguments, Path.GetTempPath());

            if (result.ExitCode != 0)
            {
                throw new Exception(string.Format("Could not create system databases for gear {0}: rc={1}; out={2}; err={3}", this.container.Uuid, result.ExitCode, result.StdOut, result.StdErr));
            } 
            
            File.WriteAllText(Path.Combine(this.container.ContainerDir, cartName, "bin", string.Format("{0}.Instance{1}", instanceType, prison.Rules.UrlPortAccess), "sqlpasswd"), newSAPassword);

            Logger.Debug("Sys db generator result for gear {0}: rc={1}; out={2}; err={3}", this.container.Uuid, result.ExitCode, result.StdOut, result.StdErr);

            // TODO: vladi: GLOBAL LOCK
        }

        public void ChangeConfigControl(string cartName, string softwareVersion, string instanceType)
        {
            try
            {
                string configPath = Path.Combine(this.container.ContainerDir, cartName, "bin", "control.exe.config");
                System.Xml.XmlDocument config = new System.Xml.XmlDocument();
                config.Load(configPath);

                foreach (System.Xml.XmlElement item in config.DocumentElement)
                {
                    if (item.Name == "appSettings")
                    {
                        item.ChildNodes[0].Attributes[1].Value = softwareVersion;
                        item.ChildNodes[1].Attributes[1].Value = instanceType;
                    }
                }

                config.Save(configPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }

        public string PostConfigure(string cartridgeName)
        {
            StringBuilder output = new StringBuilder();
            
            string name = cartridgeName.Split('-')[0];
            string version = cartridgeName.Split('-')[1];
            Manifest cartridge = GetCartridge(cartridgeName);

            if (EmptyRepository())
            {
                output.AppendLine("CLIENT_MESSAGE: An empty Git repository has been created for your application.  Use 'git push' to add your code.");
            }
            else
            {
                output.AppendLine(this.StartCartridge("start", cartridge, new Dictionary<string, object>() { { "user_initiated", true } }));
            }
            // TODO call post_install

            return output.ToString();
        }

        public string PostInstall(dynamic cartridge, string softwareVersion, dynamic options = null)
        {
            return CartridgeAction(cartridge, "post_install", softwareVersion);
        }

        public Manifest GetCartridge(string cartName)
        {
            if (!cartridges.ContainsKey(cartName))
            {
                string cartDir = string.Empty;
                try
                {
                    cartDir = CartridgeDirectory(cartName);
                    this.cartridges[cartName] = GetCartridgeFromDirectory(cartDir);
                }
                catch(Exception e)
                {
                    Logger.Error(e.ToString());
                    throw new Exception(string.Format("Failed to get cartridge {0} from {1} in gear {2}: {3}", cartName, cartDir, this.container.Uuid, e.Message));
                }
            }
            return cartridges[cartName];
        }

        public string CartridgeDirectory(string cartName)
        {
            string name = string.Empty;
            if (cartName.Split('-').Length > 1)
            {
                name = cartName.Split('-')[0];
            }
            else
            {
                name = cartName;
            }
            
            return Path.Combine(container.ContainerDir, name);
        }

        public Manifest GetCartridgeFromDirectory(string cartDir)
        {
            if(string.IsNullOrEmpty(cartDir))
            {
                throw new ArgumentNullException("Directory name is required");
            }

            if (!this.cartridges.ContainsKey(cartDir))
            {
                string cartPath = Path.Combine(container.ContainerDir, cartDir);
                string manifestPath = Path.Combine(cartPath, "metadata", "manifest.yml");
                string identPath = Directory.GetFiles(Path.Combine(cartPath, "env"), "OPENSHIFT_*_IDENT").FirstOrDefault();

                if(!File.Exists(manifestPath))
                {
                    throw new Exception(string.Format("Cartridge manifest not found: {0} missing", manifestPath));
                }

                if(identPath == null)
                {
                    throw new Exception(string.Format("Cartridge Ident not found in {0}", cartPath));
                }

                string version = Manifest.ParseIdent(File.ReadAllText(identPath))[2];
                Manifest cartridge = new Manifest(manifestPath, version, "file", this.container.ContainerDir);
                this.cartridges[cartDir] = cartridge;
            }
            return this.cartridges[cartDir];
        }

        private void CreateCartridgeDirectory(Manifest cartridge, string softwareVersion)
        {
            string target = Path.Combine(this.container.ContainerDir, cartridge.Dir);
            CartridgeRepository.InstantiateCartridge(cartridge, target);

            string ident = Manifest.BuildIdent(cartridge.CartridgeVendor, cartridge.Name, softwareVersion, cartridge.CartridgeVersion);
            Dictionary<string, string> envs = new Dictionary<string, string>();
            envs[string.Format("{0}_DIR", cartridge.ShortName)] = target + Path.DirectorySeparatorChar;
            envs[string.Format("{0}_IDENT", cartridge.ShortName)] = ident;
            WriteEnvironmentVariables(Path.Combine(target, "env"), envs);
            envs = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(this.container.Namespace))
            {
                envs["namespace"] = this.container.Namespace;
            }

            Dictionary<string, string> currentGearEnv = Environ.ForGear(this.container.ContainerDir);
            if (!currentGearEnv.ContainsKey("OPENSHIFT_PRIMARY_CARTRIDGE_DIR"))
            {
                envs["PRIMARY_CARTRIDGE_DIR"] = target + Path.DirectorySeparatorChar;
            }
            if (envs.Count > 0)
            {
                WriteEnvironmentVariables(Path.Combine(this.container.ContainerDir, ".env"), envs);
            }

            var prison = Prison.Prison.LoadPrisonNoAttach(PrisonIdConverter.Generate(this.container.Uuid));
            Logger.Debug("Setting permisions to dir {0}, prison user {1}", target, prison.User.Username);
            
            LinuxFiles.TakeOwnership(target, prison.User.Username);

            Logger.Info("Created cartridge directory {0}/{1}", container.Uuid, cartridge.Dir);
        }

        public static void WriteEnvironmentVariables(string path, Dictionary<string,string> envs, bool prefix)
        {
            Directory.CreateDirectory(path);
            foreach (KeyValuePair<string, string> pair in envs)
            {
                string name = pair.Key.ToUpper();
                if (prefix)
                {
                    name = string.Format("OPENSHIFT_{0}", name);
                }
                File.WriteAllText(Path.Combine(path, name), pair.Value);
            }
        }

        public static void WriteEnvironmentVariables(string path, Dictionary<string, string> envs)
        {
            WriteEnvironmentVariables(path, envs, true);
        }

        private bool EmptyRepository()
        {
            return new ApplicationRepository(this.container).Empty();
        }

        public string CartridgeAction(Manifest cartridge, string action, string softwareVersion, bool renderErbs = false)
        {
            string cartridgeHome = Path.Combine(this.container.ContainerDir, cartridge.Dir);
            bool ps = false;
            if (File.Exists(Path.Combine(cartridgeHome, "bin", action + ".exe")))
            {
                action = Path.Combine(cartridgeHome, "bin", action + ".exe");
            }
            else
            {
                action = Path.Combine(cartridgeHome, "bin", action + ".ps1");
                ps = true;
            }
            if (!File.Exists(action))
            {
                return string.Empty;
            }

            Dictionary<string, string> gearEnv = Environ.ForGear(this.container.ContainerDir);
            string cartridgeEnvHome = Path.Combine(cartridgeHome, "env");
            Dictionary<string, string> cartridgeEnv = Environ.Load(cartridgeEnvHome);
            cartridgeEnv.Remove("PATH");
            foreach (var kvp in gearEnv)
            {
                cartridgeEnv[kvp.Key] = kvp.Value;
            }
            if (renderErbs)
            {
                // TODO: render erb
            }

            // TODO: vladi: implement hourglass
            string cmd = null;
            if (ps)
            {
                cmd = string.Format("{0} -ExecutionPolicy Bypass -InputFormat None -noninteractive -file {1} --version {2}", ProcessExtensions.Get64BitPowershell(), action, softwareVersion);
            }
            else
            {
                cmd = string.Format("{0} --version {1}", action, softwareVersion);
            }
            string output = this.container.RunProcessInContainerContext(cartridgeHome, cmd, 0).StdOut;

            // TODO: vladi: add logging
            return output;
        }

        public string DoActionHook(string action, Dictionary<string, string> env, dynamic options)
        {
            StringBuilder output = new StringBuilder();

            action = action.Replace('-', '_');
            string actionHook = Path.Combine(env["OPENSHIFT_REPO_DIR"], ".openshift", "action_hooks", action + ".ps1");
            if (File.Exists(actionHook))
            {
                string cmd = string.Format("{0} -ExecutionPolicy Bypass -InputFormat None -noninteractive -file {1}", ProcessExtensions.Get64BitPowershell(), actionHook);

                ProcessResult processResult = container.RunProcessInContainerContext(container.ContainerDir, cmd);

                output.AppendLine(processResult.StdOut);
                output.AppendLine(processResult.StdErr);
            }
            return output.ToString();
        }

        public string ConnectorExecute(string cartName, string hookName, string publishingCartName, string connectionType, string inputArgs)
        {
            // TODO: this method is not fully implemented - its Linux counterpart has extra functionality
            Manifest cartridge = GetCartridge(cartName);
            
            bool envVarHook = (connectionType.StartsWith("ENV:") && !string.IsNullOrEmpty(publishingCartName));

            if (envVarHook)
            {
                SetConnectionHookEnvVars(cartName, publishingCartName, inputArgs);
            }

            PubSubConnector connector = new PubSubConnector(connectionType, hookName);

            if (connector.Reserved)
            {
                MethodInfo action = this.GetType().GetMethod(connector.ActioName);

                if (action != null)
                {
                    return action.Invoke(this, new object[] { cartridge, inputArgs }).ToString();
                }
                else
                {
                    // TODO: log debug info
                }
            }

            string cartridgeHome = Path.Combine(this.container.ContainerDir, cartridge.Dir);
            string script = Path.Combine(cartridgeHome, "hooks", string.Format("{0}.bat", connector.Name));

            if (!File.Exists(script))
            {
                if (envVarHook)
                {
                    return "Set environment variables successfully";
                }
                else
                {
                    throw new InvalidOperationException(string.Format("ERROR: action '{0}' not found", hookName));
                }
            }

            // TODO: vladi: add hourglass
            ProcessResult processResult = this.container.RunProcessInContainerContext(this.container.ContainerDir, string.Format("cd {0} ; {1} {2}", cartridgeHome, script, inputArgs));

            if (processResult.ExitCode == 0)
            {
                // TODO: vladi: add logging
                return processResult.StdOut;
            }

            // TODO: vladi: add error logging
            throw new Exception(string.Format("Control action '{0}' returned an error. rc={1}\n{2}", connector, processResult.ExitCode, processResult.StdErr));
        }

        private void SetConnectionHookEnvVars(string cartName, string pubCartName, string args)
        {
            string envPath = Path.Combine(this.container.ContainerDir, ".env", ShortNameFromFullCartName(pubCartName));

            object[] argsObj = JsonConvert.DeserializeObject<object[]>(args);

            string envVars = (string)((Newtonsoft.Json.Linq.JObject)argsObj[3]).Properties().ElementAt(0).Value;

            string[] pairs = envVars.Split('\n');

            Dictionary<string, string> variables = new Dictionary<string, string>();

            foreach (string pair in pairs)
            {
                if (!string.IsNullOrEmpty(pair))
                {
                    string[] keyAndValue = pair.Trim().Split('=');
                    variables[keyAndValue[0]] = keyAndValue[1];
                }
            }

            WriteEnvironmentVariables(envPath, variables, false);
        }

        public string CartridgeName { get; set; }

        public static string ShortNameFromFullCartName(string pubCartName)
        {
            if (string.IsNullOrEmpty(pubCartName))
            {
                throw new ArgumentNullException("pubCartName");
            }

            if (!pubCartName.Contains('-'))
            {
                return pubCartName;
            }

            string[] tokens = pubCartName.Split('-');

            return string.Join("-", tokens.Take(tokens.Length - 1));
        }
        
        private void CreateStopLock()
        {
            if (!StopLockExists)
            {
                File.Create(StopLock).Dispose();
                container.SetRWPermissions(StopLock);
            }
        }

        internal string Tidy()
        {
            StringBuilder output = new StringBuilder();
            EachCartridge(delegate(Manifest cartridge)
            {
                output.AppendLine(DoControl("tidy", cartridge));
            });
            return output.ToString();
        }

        internal string Deconfigure(string cartName)
        {
            StringBuilder output = new StringBuilder();
            Manifest cartridge = null;
            try
            {
                cartridge = GetCartridge(cartName);
            }
            catch
            {
                output.AppendLine(string.Format("CLIENT_ERROR: Corrupted cartridge {0} removed. There may be extraneous data left on system.", cartName));
                string name = cartName.Split('-')[0];
                string version = cartName.Split('-')[1];
                try
                {
                    cartridge = GetCartridgeFallback(cartName);
                }
                catch
                {
                    cartridge = CartridgeRepository.Instance.Select(name, version);
                }

                string ident = Manifest.BuildIdent(cartridge.CartridgeVendor, cartridge.Name, version, cartridge.CartridgeVersion);
                WriteEnvironmentVariables(Path.Combine(this.container.ContainerDir, cartridge.Dir, "env"),
                    new Dictionary<string, string>() { { string.Format("{0}_IDENT", cartridge.ShortName), ident } });
            }

            try
            {
                StopCartridge(cartridge, new Dictionary<string, object>() { { "user_initiated", true } });
                output.AppendLine(CartridgeTeardown(cartridge.Dir, true));
            }
            catch(Exception e)
            {
                output.AppendLine(Utils.Sdk.TranslateOutForClient(e.Message, Utils.Sdk.ERROR));
            }
            finally
            {
                DeleteCartridgeDirectory(cartridge);
            }
            return output.ToString();
        }

        private void DeleteCartridgeDirectory(Manifest cartridge)
        {
            Directory.Delete(Path.Combine(this.container.ContainerDir, cartridge.Dir), true);
        }

        private Manifest GetCartridgeFallback(string cartName)
        {
            string directory = CartridgeDirectory(cartName);
            string version = cartName.Split('-')[1];
            string cartridgePath = Path.Combine(this.container.ContainerDir, directory);
            string manifestPath = Path.Combine(cartridgePath, "metadata", "manifest.yml");
            return new Manifest(manifestPath, version, null, this.container.ContainerDir, true);
        }

        public void CreatePrivateEndpoints(Manifest cartridge)
        {
            if (cartridge == null)
            {
                throw new ArgumentNullException("cartridge");
            }

            if (cartridge.Endpoints == null || cartridge.Endpoints.Count == 0)
            {
                return;
            }

            foreach (Endpoint endpoint in cartridge.Endpoints)
            {
                string privateIp = "0.0.0.0";
                container.AddEnvVar(endpoint.PrivateIpName, privateIp);

                string port = container.ReadEnvVar("PRISON_PORT");

                if (string.IsNullOrWhiteSpace(port))
                {
                    Logger.Error("No prison port available for gear {0}", this.container.Uuid);
                    throw new Exception(string.Format("No prison port available for gear {0}", this.container.Uuid));
                }

                container.AddEnvVar(endpoint.PrivatePortName, port);

                //if (!string.IsNullOrWhiteSpace(endpoint.WebsocketPortName) && !string.IsNullOrWhiteSpace(endpoint.WebsocketPort))
                //{
                //    string websocketPort = endpoint.WebsocketPort == "0" ? Network.GrabEphemeralPort().ToString() : endpoint.WebsocketPort;
                //    container.AddEnvVar(endpoint.WebsocketPortName, websocketPort);
                //}
            }
        }

        internal void CreateDependencyDirectories(Manifest cartridge)
        {
            // TODO
            // need managed_files.yml from here on
            return;

            foreach (string dependenciesDirName in new string[] { "build-dependencies", "dependencies" })
            {
                List<string> dirs = null;
                if(dependenciesDirName == "build-dependencies")
                {
                    dirs = this.container.BuildDependencyDirs(cartridge);
                }

            }
        }
    }
}
