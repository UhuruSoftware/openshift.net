using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.Openshift.Runtime;
using System.IO;
using System.Threading;

namespace Uhuru.Openshift.Tests.Unit
{
    [TestClass]
    public class ContainerTest
    {
        [TestMethod]
        public void Test_Configure()
        {
            ApplicationContainer container = new ApplicationContainer();
            container.Configure("dotNet", null, null);                       
        }

        [TestMethod]
        public void Test_AddSsh_Key()
        {
            ApplicationContainer container = new ApplicationContainer();
            container.AddSshKey("AAAAB3NzaC1yc2EAAAADAQABAAABAQDh1D8gtGIYjXHaRCvZZeeFRJiJICiP03d3Intc0xtSHyIieogsr8pN3awTWS7V0prlT+hp6l1Sb20Vic/az9z3kSAoCL/eu+Nfcyc6oBhkkcnz2Ag/5bzstlRJmWahxIr+LT2bbqCiUTBaNWG5oKknQWQB+gryaKqzIfX2dPtKikvpSUNi+D8quQDscUmx9gCbFy3rhZNJiguylQaBngxfwtt/7y8bjcMRvucffrEfMQkPQvH0vOBoOJpigAqJtDyvyvQ7dZvOqGUPgw+rClG3f0fzy34u9i26LutxZiFOQKHX5GxoD3GcQ0XMOZJ5PDxmj4WJkgLFXzTnqTyKkHZh", "ssh-rsa", "5278c3410798909d9c000001-default");
        }
    }
}
