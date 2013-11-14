using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uhuru.Openshift.Cmdlets.Tests.Unit
{
    [TestClass]
    public class NodeTest
    {
        [TestMethod]
        public void Test_GetCartridgeList()
        {
            string output = ClassLibrary.Node.GetCartridgeList(true, true, false);
            Assert.IsNotNull(output);
        }
    }
}
