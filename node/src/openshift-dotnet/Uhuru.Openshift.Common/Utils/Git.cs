using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.Openshift.Common.Utils
{
    public class Git
    {
        public static readonly string[] ALLOWED_SCHEMES = {"git", "git@", "http", "https", "ftp", "ftps", "rsync"};
        public const string EMPTY_CLONE_SPEC = "empty";

        public static bool EmptyCloneSpec(string url)
        {
            return string.Equals(url, EMPTY_CLONE_SPEC, StringComparison.InvariantCultureIgnoreCase);
        }

        public static void SafeCloneSpec(string url, out string repoSpec, out string commit, string[] schemes = null)
        {
            if (schemes == null)
                schemes = ALLOWED_SCHEMES;
            if(EmptyCloneSpec(url))
            {
                repoSpec = EMPTY_CLONE_SPEC;
                commit = null;
                return;
            }
            Uri uri = new Uri(url);
            if (!schemes.Contains(uri.Scheme))
            {
                repoSpec = null;
                commit = null;
                return;
            }
            commit = uri.Fragment;
            UriBuilder ub = new UriBuilder(uri);
            ub.Fragment = null;
            repoSpec = ub.ToString();
        }
    }
}
