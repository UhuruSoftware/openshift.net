using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.Openshift.Common;
using System.IO;
using System.Linq;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class LoggerTest
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void TestLogsOK()
        {
            // Arrange
            Logger.LogFile = Path.GetTempFileName();

            // Act
            Logger.Debug("test message");

            // Assert
            Assert.IsTrue(File.Exists(Logger.LogFile));            
        }
    }
}
