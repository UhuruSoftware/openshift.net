using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.Openshift.Common;
using System.IO;
using System.Linq;
using Uhuru.Openshift.Runtime.Config;
using Uhuru.Openshift.Runtime;
using Uhuru.Openshift.Utilities;

namespace Uhuru.Openshift.Tests.Integration
{
    /// <summary>
    /// These tests assume there is a node.conf, and that ruby is properly setup
    /// </summary>
    [TestClass]
    public class FacterTest
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void TestFacterOK()
        {
            // Arrange

            // Act
            RubyHash facts = Uhuru.Openshift.Runtime.Utils.Facter.GetFacterFacts();

            // Assert
            Assert.IsTrue(facts.ContainsKey("operatingsystem"));
            Assert.AreEqual(facts["operatingsystem"], "windows");
        }
    }
}
