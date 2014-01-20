using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Uhuru.Openshift.Common.JsonHelper;

namespace Uhuru.Openshift.Runtime.Model.ApplicationContainerExt
{
    public class AuthorizedKeysFile
    {
        ApplicationContainer Container { get; set; }
        string Filename { get; set; }

        public AuthorizedKeysFile(ApplicationContainer container, string filename = null)
        {
            this.Container = container;
            this.Filename = string.IsNullOrEmpty(filename) ? Path.Combine(this.Container.ContainerDir, ".ssh", "authorized_keys") : filename;
        }

        public void ReplaceKeys(List<SshKey> newKeys)
        {
            Modify(delegate(Dictionary<string, SshKey> keys)
            {
                keys.Clear();
                foreach (SshKey key in newKeys)
                {
                    string id = KeyId(key.Comment);
                    keys[id] = new SshKey() { Comment = key.Comment, Key = key.Key, Type = key.Type };
                }
            });
        }

        string KeyId(string comment)
        {
            return string.Format("OPENSHIFT-{0}-{1}", this.Container.Uuid, comment);
        }

        public delegate void ModifyCallback(Dictionary<string, SshKey> sshKeys);
        public void Modify(ModifyCallback action)
        {
            Dictionary<string, SshKey> keys = new Dictionary<string, SshKey>();
            File.Copy(this.Filename, this.Filename + ".bak");
            try
            {
                using (StreamReader sr = new StreamReader(this.Filename))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            if (line.Trim() != string.Empty)
                            {
                                string[] values = line.Split(' ');
                                SshKey sshKey = new SshKey() { Type = values[0], Key = values[1], Comment = values[2] };
                                keys[sshKey.Comment] = sshKey;
                            }
                        }
                    }
                }
                Dictionary<string, SshKey> oldKeys = new Dictionary<string, SshKey>(keys);
                
                action(keys);

                // TODO compare new keys with old keys before overwriting

                using (File.Create(this.Filename)) { }
                using (StreamWriter sw = new StreamWriter(this.Filename))
                {
                    foreach (SshKey value in keys.Values)
                    {
                        sw.WriteLine(string.Format("{0} {1} {2}", value.Type, value.Key, value.Comment));
                    }
                }
            }
            catch (Exception ex)
            {
                File.Copy(this.Filename + ".bak", this.Filename, true);
                throw ex;
            }
            finally
            {
                File.Delete(this.Filename + ".bak");
            }
        }
    }
}
