using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.Openshift.Common;
using System.IO;
using System.Linq;
using Uhuru.Openshift.Common.Utils;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class UniquePredictablePortTest
    {
       
        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GetsAPort()
        {
            // Arrange
            int availablePort = 0;

            // Act
            availablePort = Network.GetUniquePredictablePort("myfile");

            // Assert
            Assert.AreNotEqual(0, availablePort);
            Assert.IsTrue(availablePort > 10000);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Test_GetsMultipleUniquePorts()
        {
            // Arrange
            int availablePort1 = 0;
            int availablePort2 = 0;

            // Act
            availablePort1 = Network.GetUniquePredictablePort("myfile");
            availablePort2 = Network.GetUniquePredictablePort("myfile");

            // Assert
            Assert.AreNotEqual(0, availablePort1);
            Assert.IsTrue(availablePort1 > 10000);
            Assert.AreNotEqual(0, availablePort2);
            Assert.IsTrue(availablePort2 > 10000);

            Assert.AreNotEqual(availablePort1, availablePort2);
        }
    }
}
