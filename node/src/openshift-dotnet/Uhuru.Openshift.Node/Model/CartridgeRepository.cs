using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Uhuru.Openshift.Common.Models;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime.Utils;
using Uhuru.Openshift.Utilities;
using YamlDotNet.RepresentationModel.Serialization;

namespace Uhuru.Openshift.Runtime
{
    public class CartridgeRepository: IEnumerable
    {
        string CARTRIDGE_REPO_DIR = NodeConfig.Values.Get("CARTRIDGE_REPO_DIR");
        
        public string path;
        static object OpenShiftCartridgeRepositorySemaphore = new object();

        private static CartridgeRepository instance;
        Dictionary<string, Dictionary<string, Dictionary<string, Manifest>>> Index;

        private CartridgeRepository() 
        {
            this.path = CARTRIDGE_REPO_DIR;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Clear();
            Load(path);
        }

        private void Load(string directory = null)
        {
            lock (OpenShiftCartridgeRepositorySemaphore)
            {
                bool loadViaUrl = directory == null;
                FindManifests(directory ?? path, delegate(string manifestPath)
                {
                    Logger.Debug("Loading cartridge from {0}", manifestPath);

                    if (new FileInfo(manifestPath).Length == 0)
                    {
                        Logger.Warning("Skipping load of {0} because manifest appears to be corrupted");
                        return;
                    }

                    Manifest c = Insert(new Manifest(manifestPath, null, "file", path, loadViaUrl));
                    Logger.Debug("Loaded cartridge ({0}, {1}, {2})", c.Name, c.Version, c.CartridgeVersion);
                });
            }
        }

        public Manifest Select(string cartridgeName, string version, string cartridgeVersion = "_")
        {
            if(!Exist(cartridgeName, version, cartridgeVersion))
            {
                throw new KeyNotFoundException(string.Format("key not found: {0}, {1}, {2}", cartridgeName, version, cartridgeVersion));
            }

            return Index[cartridgeName][version][cartridgeVersion];
        }

        public bool Exist(string cartridgeName, string version, string cartridgeVersion)
        {
            return Index.ContainsKey(cartridgeName) && Index[cartridgeName].ContainsKey(version) && Index[cartridgeName][version].ContainsKey(cartridgeVersion);
        }

        private delegate void FindManifestsCallback(string filename);
        private void FindManifests(string directory, FindManifestsCallback action)
        {
            if(!Directory.Exists(directory))
            {
                throw new ArgumentException(string.Format("Illegal path to cartridge repository: {0}", directory));
            }

            foreach(string path in Directory.GetDirectories(directory))
            {
                List<string> entries = Directory.GetDirectories(path).Select(v => new DirectoryInfo(v).Name).ToList<string>();
                if(entries.Count == 0)
                {
                    continue;
                }

                foreach(string version in Manifest.SortVersions(entries))
                {
                    string filename = Path.Combine(path, version, "metadata", "manifest.yml");
                    if(File.Exists(filename))
                    {
                        action(filename);
                    }
                }
            }


        }

        private void Clear()
        {
            Index = new Dictionary<string, Dictionary<string, Dictionary<string, Manifest>>>();
        }

        public List<Manifest> LatestVersions()
        {
            List<Manifest> cartridges = new List<Manifest>();
            foreach(string cartName in Index.Keys)
            {
                foreach(string softwareVersion in Index[cartName].Keys)
                {
                    Manifest latest = Index[cartName][softwareVersion]["_"];
                    cartridges.Add(latest);
                }
            }

            return cartridges;
        }

        public static CartridgeRepository Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CartridgeRepository();
                }
                return instance;
            }
        }        

        public Manifest Install(string directory)
        {
            if(!Directory.Exists(directory))
            {
                throw new ArgumentException(string.Format("Illegal path to cartridge source: {0}", directory));
            }

            if(directory == path)
            {
                throw new ArgumentException(string.Format("Source cannot be: {0}", path));
            }

            string manifestPath = Path.Combine(directory, "metadata", "manifest.yml");
            if(!File.Exists(manifestPath))
            {
                throw new ArgumentException(string.Format("Cartridge manifest.yml missing: {0}", manifestPath));
            }

            Manifest entry = null;
            lock(OpenShiftCartridgeRepositorySemaphore)
            {
                entry = Insert(new Manifest(manifestPath, null, "file", path));
                if(Directory.Exists(entry.RepositoryPath))
                {
                    Directory.Delete(entry.RepositoryPath, true);
                }
                Directory.CreateDirectory(entry.RepositoryPath);
                DirectoryUtil.DirectoryCopy(directory, entry.RepositoryPath, true);
            }
            return entry;
        }

        public Manifest Erase(string cartridgeName, string version, string cartridgeVersion, bool force = false)
        {
            if (!Exist(cartridgeName, version, cartridgeVersion))
            {
                throw new KeyNotFoundException(string.Format("key not found: {0}, {1}, {2}", cartridgeName, version, cartridgeVersion));
            }

            if(!force && InstalledInBasePath(cartridgeName, version, cartridgeVersion))
            {
                throw new Exception("Cannot erase cartridge installed in CARTRIDGE_BASE_PATH");
            }

            Manifest entry = null;
            lock(OpenShiftCartridgeRepositorySemaphore)
            {
                entry = Select(cartridgeName, version, cartridgeVersion);
                foreach(string softwareVersion in entry.Versions)
                {
                    Remove(cartridgeName, softwareVersion, cartridgeVersion);
                }

                string parent = new DirectoryInfo(entry.RepositoryPath).Parent.FullName;
                Directory.Delete(entry.RepositoryPath, true);
                if(!Directory.GetFileSystemEntries(parent).Any())
                {
                    Directory.Delete(parent, true);
                }
            }
            return entry;
        }

        public bool InstalledInBasePath(string cartridgeName, string version, string cartridgeVersion)
        {
            string cartridgePath = Path.Combine(CartridgeBasePath, cartridgeName);
            if(!Directory.Exists(cartridgePath))
            {
                return false;
            }

            string manifestPath = Path.Combine(cartridgePath, "metadata", "manifest.yml");
            if(!File.Exists(manifestPath))
            {
                return false;
            }

            bool error = false;

            Manifest manifest = null;
            try
            {
                manifest = new Manifest(manifestPath, null, "file");
            }
            catch
            {
                error = true;
            }

            return (!error && manifest.Versions.Contains(version) && manifest.CartridgeVersion == cartridgeVersion);
        }

        public void Remove(string cartridgeName, string version, string cartridgeVersion)
        {
            bool recomputeCartridgeVersion = false;
            if (!Exist(cartridgeName, version, cartridgeVersion))
            {
                throw new KeyNotFoundException(string.Format("key not found: {0}, {1}, {2}", cartridgeName, version, cartridgeVersion));
            }

            Logger.Debug("Removing ({0}, {1}, {2}) from index", cartridgeName, version, cartridgeVersion);

            Dictionary<string, Dictionary<string, Manifest>> slice = Index[cartridgeName];

            if(LatestInSlice(slice[version], cartridgeVersion))
            {
                recomputeCartridgeVersion = true;
            }

            slice[version].Remove(cartridgeVersion);
            List<string> realCartVersions = slice[version].Keys.ToList();
            realCartVersions.Remove("_");

            if(realCartVersions.Count == 0)
            {
                Logger.Debug("No more cartridge versions for ({0}, {1}, deleting from index", cartridgeName, version);
                slice.Remove(version);
                recomputeCartridgeVersion = false;

                if(slice.Count == 0)
                {
                    Logger.Debug("No more versions left for {0} deleting from index", cartridgeName);
                    Index.Remove(cartridgeName);
                }
            }

            if(Index.ContainsKey(cartridgeName) && recomputeCartridgeVersion)
            {
                string latestCartridgeVersion = LatestInSlice(slice[version]);
                if(latestCartridgeVersion != null)
                {
                    Logger.Debug("Resetting default for ({0}, {1} to {2}", cartridgeName, version, latestCartridgeVersion);
                    Manifest manifest = Index[cartridgeName][version][latestCartridgeVersion];
                    Index[cartridgeName][version]["_"] = manifest;
                }
            }
        }

        public Manifest Insert(Manifest cartridge)
        {
            string name = cartridge.Name;
            string cartridgeVersion = cartridge.CartridgeVersion;

            foreach(string version in Manifest.SortVersions(cartridge.Versions))
            {
                Manifest projectedCartridge = cartridge.ProjectVersionOverrides(version, path);
                
                if (!Index.ContainsKey(name))
                {
                    Index[name] = new Dictionary<string, Dictionary<string, Manifest>>();
                }

                Index[name][version] = new Dictionary<string, Manifest>();
                Index[name][version][cartridgeVersion] = projectedCartridge;
                Index[name][version]["_"] = projectedCartridge;
            }

            return cartridge;
        }

        public static string CartridgeBasePath
        {
            get
            {
                return NodeConfig.Values["CARTRIDGE_BASE_PATH"];
            }
        }

        public bool LatestInSlice(Dictionary<string, Manifest> indexSlice, string version)
        {
            return LatestInSlice(indexSlice) == version;
        }

        public string LatestInSlice(Dictionary<string, Manifest> indexSlice)
        {
            List<string> realVersions = indexSlice.Keys.ToList();
            realVersions.RemoveAll(v => v == "_");
            return Manifest.SortVersions(realVersions).LastOrDefault();
        }

        public bool LatestCartridgeVersion(string cartridgeName, string version, string cartridgeVersion)
        {
            if(!Exist(cartridgeName, version, cartridgeVersion))
            {
                return false;
            }

            return LatestInSlice(Index[cartridgeName][version], cartridgeVersion);
        }

        public IEnumerator GetEnumerator()
        {
            HashSet<Manifest> cartridges = new HashSet<Manifest>();

            foreach(Dictionary<string, Dictionary<string, Manifest>> sw in Index.Values)
            {
                foreach (Dictionary<string, Manifest> cart in sw.Values)
                {
                    foreach(Manifest cartridge in cart.Values)
                    {
                        if (cartridge != null)
                        {
                            cartridges.Add(cartridge);
                        }
                    }
                }
            }

            foreach(Manifest cartridge in cartridges)
            {
                yield return cartridge;
            }
        }

        public string Inspect()
        {
            StringBuilder output = new StringBuilder();

            output.AppendLine("<CartridgeRepository:");
            foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, Manifest>>> sw in Index)
            {
                string name = sw.Key;
                foreach(KeyValuePair<string, Dictionary<string, Manifest>> cart in sw.Value)
                {
                    string version = cart.Key;
                    foreach(KeyValuePair<string, Manifest> cartridge in cart.Value)
                    {
                        string cartVersion = cartridge.Key;
                        output.AppendLine(string.Format("({0}, {1}, {2}): {3}", name, version, cartVersion, cartridge.Value.ToString()));
                    }
                }
            }
            output.AppendLine(">");
            return output.ToString();
        }

        public string ToString()
        {
            StringBuilder output = new StringBuilder();
            foreach(Manifest c in this)
            {
                output.AppendLine(string.Format("({0}, {1}, {2}, {3})", c.CartridgeVendor, c.Name, c.Version, c.CartridgeVersion));
            }
            return output.ToString();
        }

        public static void  OverlayCartridge(Manifest cartridge, string target)
        {
            InstantiateCartridge(cartridge, target, false);
        }

        public static void InstantiateCartridge(Manifest cartridge, string target, bool failureRemove = true)
        {
            Directory.CreateDirectory(target);
            try
            {
                bool downloadable = cartridge.ManifestPath == "url";

                if (downloadable)
                {
                    Uri uri = new Uri(cartridge.SourceUrl);
                    string temporary = Path.Combine(target, Path.GetFileName(uri.LocalPath));

                    if (uri.Scheme == "git" || cartridge.SourceUrl.EndsWith(".git"))
                    {
                        // use intermediate temp directory to reset cygwin directories ACLs
                        string tempRepo = cartridge.Name + ".temp";
                        string tempRepoDir = Path.Combine(new DirectoryInfo(target).Parent.FullName, tempRepo);
                        try
                        {
                            string template = @"{0} clone {1} {2}
set GIT_DIR=./{2}/.git
{0} repack";
                            string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "cmd.bat");
                            File.WriteAllText(file, string.Format(template, Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], @"bin\git.exe"), cartridge.SourceUrl, tempRepo));
                            ProcessResult result = ProcessExtensions.RunCommandAndGetOutput("cmd.exe", string.Format("/c {0}", file), new DirectoryInfo(target).Parent.FullName);
                            if (result.ExitCode != 0)
                            {
                                throw new Exception(string.Format("Unable to clone cartridge from {0} stdout: {1} stderr {2}", cartridge.SourceUrl, result.StdOut, result.StdErr));
                            }

                            DirectoryUtil.DirectoryCopy(tempRepoDir, target, true);
                        }
                        finally
                        {
                            if (Directory.Exists(tempRepoDir))
                            {
                                Directory.Delete(tempRepoDir, true);
                            }
                        }                        
                    }
                    else if (Regex.IsMatch(uri.Scheme, @"^https*") && Regex.IsMatch(cartridge.SourceUrl, @"\.zip"))
                    {
                        try
                        {
                            UriCopy(new Uri(cartridge.SourceUrl), temporary, cartridge.SourceMD5);
                            Extract("zip", temporary, target);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex.ToString());
                            throw ex;
                        }
                        finally
                        {
                            if (File.Exists(temporary))
                            {
                                File.Delete(temporary);
                            }
                        }
                    }
                    else if (Regex.IsMatch(uri.Scheme, @"^https*") && Regex.IsMatch(cartridge.SourceUrl, @"(\.tar\.gz|\.tgz)$"))
                    {
                        try
                        {
                            UriCopy(new Uri(cartridge.SourceUrl), temporary, cartridge.SourceMD5);
                            Extract("tgz", temporary, target);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex.ToString());
                            throw ex;
                        }
                        finally
                        {
                            if (File.Exists(temporary))
                            {
                                File.Delete(temporary);
                            }
                        }
                    }
                    else if (Regex.IsMatch(uri.Scheme, @"^https*") && Regex.IsMatch(cartridge.SourceUrl, @"\.tar$"))
                    {
                        try
                        {
                            UriCopy(new Uri(cartridge.SourceUrl), temporary, cartridge.SourceMD5);
                            Extract("tar", temporary, target);
                        }
                        catch(Exception ex)
                        {
                            Logger.Error(ex.ToString());
                            throw ex;
                        }
                        finally
                        {
                            if (File.Exists(temporary))
                            {
                                File.Delete(temporary);
                            }
                        }
                    }
                    else if (uri.Scheme == "file")
                    {
                        DirectoryUtil.DirectoryCopy(uri.LocalPath, target, true);
                    }
                    else
                    {
                        throw new ArgumentException(string.Format("CLIENT_ERROR: Unsupported URL({0}) for downloading a private cartridge", cartridge.SourceUrl));
                    }
                }
                else
                {
                    // TODO exclude usr folder and use link
                    DirectoryUtil.DirectoryCopy(cartridge.RepositoryPath, target, true);
                }

                ValidateCartridgeHome(cartridge, target);

                if (downloadable)
                {
                    string manifestOnDisk = Path.Combine(target, "metadata", "manifest.yml");
                    using (StreamWriter sw = new StreamWriter(manifestOnDisk))
                    {
                        Serializer ser = new Serializer(SerializationOptions.None);
                        ser.Serialize(sw, cartridge.ManifestSpec);
                    }
                }
            }
            catch(Exception e)
            {
                if (failureRemove)
                {
                    if (Directory.Exists(target))
                    {
                        Directory.Delete(target);
                    }
                }
                throw e;
            }
        }

        private static void ValidateCartridgeHome(Manifest cartridge, string path)
        {
            List<string> errors = new List<string>();
            if(!Directory.Exists(Path.Combine(path, "metadata")))
            {
                errors.Add(Path.Combine(path, "metadata") + "is not a directory");
            }
            if (!Directory.Exists(Path.Combine(path, "bin")))
            {
                errors.Add(Path.Combine(path, "bin") + "is not a directory");
            }
            if (!File.Exists(Path.Combine(path, "metadata", "manifest.yml")))
            {
                errors.Add(Path.Combine(path, "metadata", "manifest.yml") + "is not a file");
            }
            if(errors.Count != 0)
            {
                throw new MalformedCartridgeException(string.Format("CLIENT_ERROR: Malformed cartridge ({0}, {1}, {2})", cartridge.Name, cartridge.Version, cartridge.CartridgeVersion), errors.ToArray());
            }
        }

        private static void UriCopy(Uri uri, string temporary, string md5 = null)
        {
            WebClient client = new WebClient();
            client.DownloadFile(uri, temporary);
            if(md5 != null)
            {
                //TODO check md5
            }
        }

        private static void Extract(string method, string source, string target)
        {
            // use intermediate temp directory to reset cygwin directories ACLs
            string tempPath = target + ".temp";
            Directory.CreateDirectory(tempPath);
            Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], @"bin\tar.exe");
            Logger.Debug("Extracting {0} to {2} using {1} method", source, method, target);
            switch (method)
            {
                case "zip":
                    {
                        ZipFile.ExtractToDirectory(source, tempPath);
                        break;
                    }
                case "tgz":
                    {
                        ProcessResult result = ProcessExtensions.RunCommandAndGetOutput(Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], @"bin\tar.exe"), string.Format("-C {0} -zxpf {1}", LinuxFiles.Cygpath(tempPath), LinuxFiles.Cygpath(source)));
                        Logger.Debug(result.StdOut + result.StdErr);
                        break;
                    }
                case "tar":
                    {
                        ProcessResult result = ProcessExtensions.RunCommandAndGetOutput(Path.Combine(NodeConfig.Values["SSHD_BASE_DIR"], @"bin\tar.exe"), string.Format("-C {0} -xpf {1}", LinuxFiles.Cygpath(tempPath), LinuxFiles.Cygpath(source)));
                        Logger.Debug(result.StdOut + result.StdErr);
                        break;
                    }
                default:
                    {
                        throw new Exception(string.Format("Packaging method {0} not yet supported.", method));
                    }
            }

            DirectoryUtil.DirectoryCopy(tempPath, target, true);
            Directory.Delete(tempPath, true);

            string[] files = Directory.GetFileSystemEntries(target);
            if(files.Length == 1)
            {
                Logger.Debug(string.Join(",", files));
                // A directory of one file is not legal move everyone up a level (github zip's are this way)
                string to_delete = files[0] + ".to_delete";
                Directory.Move(files[0], to_delete);
                DirectoryUtil.DirectoryCopy(to_delete, target, true);
                Directory.Delete(to_delete);
            }

        }
    }

    class MalformedCartridgeException : Exception
    {
        string[] details;

        public MalformedCartridgeException(string message = null, string[] details = null)
        {
            if(details == null)
            {
                details = new string[] {};
            }
            else
            {
                this.details = details;
            }
        }

        public override string ToString()
        {
            return this.Message + string.Join(", ", details);
        }
    }
}
