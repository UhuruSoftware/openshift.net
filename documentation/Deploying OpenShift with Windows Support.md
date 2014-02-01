# Deploying OpenShift with Windows support #

This document provides instructions on how to deploy a 2 node OpenShift environment that has Windows support. 
We are going to use an Origin Fedora VM that comes with a full installation - a broker and a node on the same VM.

Next to this we're going to setup a Windows VM that will connect to the Linux OpenShift broker. 

## Cloud Topology ##


## Setting up things on Linux ##

Linux VM download location

RPM fedora location
RPMs can be found [here](http://winjenkins.hosts.uhuruos.com/). Credentials are required to download the packages.

Installation:

	wget http://<user>:<password>@winjenkins.hosts.uhuruos.com/uhuruorigin-0.<version>.rpm
	yum remove uhuruorigin
	yum install uhuruorigin-0.<version>.rpm
	service mcollective restart
	service openshift-broker restart
	(cd /var/www/openshift/broker/; bundle exec rake tmp:clear)

## Windows Prerequisites ##

1. Windows Version

	The supported Windows versions are Windows Server 2012 and Windows Server 2012 R2
- Group policy settings add Local Users to allow creating symlinks

mmc -> add snap in -> group policies -> 

- IIS

	Required features:

- SQL Server 2008

Needs to be installed with a default instance, with mixed authentication, and please note the password you set for the 'sa' account.

- mDNS (no solution, use hosts file for now)


- Build Tools


## Configuring domains ##

## Using the Windows Install script ##


	E:\Code\openshift.net>build.bat & powershell -File E:\code\openshift.net\output\powershell\Tools\openshift.net\install.p
	s1 -publicHostname w-vladi2.openshift.local -brokerHost broker-a211bd.openshift.local -sqlServerSAPassword password1234!
	 -cloudDomain openshift.local -publicIP 10.2.0.104 -externalEthDevice "Ethernet 2" -internalEthDevice "Ethernet 2" -skip
	Ruby -skipMCollective -skipCygwin -skipChecks -skipGlobalEnv -skipServicesSetup -skipBinDirCleanup


## Creating your first Windows application ##

All OpenShift applications that contain a Windows cartridge must be configured as scalable.
When you use `rhc` to create a Windows application, make sure to specify the `-s` flag:
 rhc create-app laser dotnet --from-code git://github.com/florindragos/dotnet-sample.git
	rhc create-app myapp dotnet -s

## FAQ ##

- How does my application topology look like?
- How is my application secured?
- How is my application built?
- How is my application deployed?
- Can I use Jenkins build?
- What services are available for my Windows application?
- How do I connect to databases from my Windows app?
- Can I install COM components or register DLLs?