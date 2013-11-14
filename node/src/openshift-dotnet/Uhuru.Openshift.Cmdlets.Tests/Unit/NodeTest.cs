using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class NodeTest
    {
        [TestMethod]
        public void Test_GetCartridgeList()
        {
            string output = Common.Node.GetCartridgeList(true, true, false);
            Assert.IsNotNull(output);
        }
    }
}
