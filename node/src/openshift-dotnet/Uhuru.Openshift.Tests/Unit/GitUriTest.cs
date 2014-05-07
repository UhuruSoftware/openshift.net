using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Openshift.Common.Utils;


namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class GitUriTest
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GitSafeCloneSpec()
        {
            bool success = false;
            string repospec=string.Empty;
            string commitinfo=string.Empty;
            Git.SafeCloneSpec("https://github.com/UhuruSoftware/openshift.net/commit/cb03ec7558d270bcfbbe0fcfd49f19d536b44acf#diff-e28114c980e5dbf340e616e8c426bfdbL24", out repospec, out commitinfo);
            if ((repospec != string.Empty) && (commitinfo != string.Empty))
            {
                success = true;
            }
            Assert.AreEqual(true, success);
        }
    }
}
