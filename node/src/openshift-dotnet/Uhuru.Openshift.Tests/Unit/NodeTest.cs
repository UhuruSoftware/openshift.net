using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class NodeTest
    {
        [TestMethod]
        public void Test_GetCartridgeList()
        {
            string output = Node.GetCartridgeList(true, true, false);
            Assert.IsNotNull(output);
        }
    }
}
