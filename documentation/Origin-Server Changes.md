# Changes to origin-server for Windows support

This document describes the changes that have to be made to openshift-origin to support Windows on OpenShift.

## Current 'PoC' Implementation

I will refer to the current changes in OpenShift origin as 'PoC', since they have not gone through review yet, and we are already identifying ways to make the implementation better. I am assuming that this document will go through quite a few iterations, and eventually the PoC information will be removed. I am including it for now as a beachhead. Where appropriate, I've included references to existing code changes.

One thing to keep in mind is that for the PoC implementation we have made changes that do not affect Linux nodes or cartridges. With the new proposed changes, this will not be the case.

This [commit](https://github.com/UhuruSoftware/origin-server/commit/ca0fdb3d9726777cf6e6f6b01ab5d064747bd83a) sums up the changes quite well. Below I've included examples for the kernel fact configuration and the Windows category in cartridges. 

#### PoC Node Fact
 
An existing MCollective fact named 'kernel' is used for filtering.

Examples:

Linux

    ---
      architecture: x86_64
      kernel: Linux
      augeasversion: "1.1.0"
      domain: openshift.local
      macaddress: "00:0c:29:61:e2:8b"
      operatingsystem: Fedora
      lsbdistid: Fedora
      facterversion: "1.6.18"
      fqdn: broker-7c50.openshift.local
      ...    

Windows

    ---
    {
      ...
      ? !!str "gears_active_usage_pct"
      : !!float "0",
      ? !!str "capacity"
      : !!str "0.0",
      ? !!str "active_capacity"
      : !!str "0.0",
      ? !!str "hostname"
      : !!str "w-vladi2",
      ? !!str "kernel"
      : !!str "Windows",
      ...


#### PoC Cartridge Categories 
 
We use a category named "windows" to identify cartridges that are meant to be deployed on a Windows OS.

Example:

    Name: dotnet
    Cartridge-Short-Name: DOTNET
	...
    License-Url: "http://www.uhurusoftware.com"
    Vendor: "Microsoft Corporation"
    Categories:
      - web_framework
      - windows
    Website: http://www.microsoft.com/
    Help-Topics:
      "Developer Center": "http://www.uhurusoftware.com/community/developers"
    ...
    

## Filtering nodes based on the OS

The fact that OpenShift is going to support both Linux and Windows nodes means the node network is no longer homogeneous, since these two operating systems can't support the same cartridges.

It follows that we need a mechanism that allows filtering the node network based on what operating system they live on.

### Node Configuration

In order to be able to tell what cartridges are supported on which nodes, a new fact named "Platform" will be added for both Windows and Linux nodes. This will require code changes to origin (specifically the part that generates the facts.yaml file for nodes). The fact itself should be generated as follows:

- On Linux: using the `lsb_release -a` command, having the format [Distributor ID]-[Release] (e.g. RedHatEnterpriseServer-6.5, Fedora-19, LinuxMint-13)
- On Windows: the 'Distributor ID' part of the platform name will always be 'Windows', and the 'Release' part will be given by the windows kernel version; e.g. Windows-6.3 (Windows 2012 R2), Windows-6.0 (Windows 2008 SP1)

### Cartridges ###

In order to properly understand the OS requirements for a cartridge, we will add a new top-level attribute in the cartridge manifest file. The attribute will be an array of strings named "Platform". Each of the strings in the array will represent an OS that is compatible with the cartridge. The string will have to match the Platform fact exposed by OpenShift nodes. 

Examples:

- a Windows cartridge

	Name: dotnet
	Platform: ['Windows-6.3']

- a Linux cartridge

	Name: ruby
	Platform: ['Fedora-19', 'RedHatEnterpriseServer-6.5']

Platform matching should be case insensitive.
We may want to consider allowing wildcards for the version part of the platform string.    

### Broker Code Changes

We need the broker to build a list of platforms available in the network. This will be required when listing cartridges. We also need the MCollective proxy to accept a new filtering option when looking up nodes, named 'Platform'.

Modified files (in the PoC):

- [`./origin-server/controller/lib/openshift/application_container_proxy.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/lib/openshift/application_container_proxy.rb)

	Modified the `find_available` and `find_one` methods to accept a kernel parameter.

- [`./origin-server/plugins/msg-broker/mcollective/lib/openshift/mcollective_application_container_proxy.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/plugins/msg-broker/mcollective/lib/openshift/mcollective_application_container_proxy.rb)

	Modified the `find_available_impl` and `find_one_impl` methods to accept a kernel parameter. Modified the `rpc_find_available` and `rpc_find_one` methods to filter nodes based on the 'kernel' fact.

## Listing cartridges

Previously, any node was selected for listing cartridges - a homogeneous network was assumed. Since this is no longer the case, the broker should keep a list of the available platforms in the node network, and query a node for each type of platform - this will compile a complete list of all available cartridges.  

Modified files (in the PoC):

- [`./origin-server/controller/app/helpers/cartridge_cache.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/app/helpers/cartridge_cache.rb)

	Modified the `get_all_cartridges` method to grab cartridges for a Linux node and then a Windows node, and then merge the results.  

## Creating Gears

Based on what type of cartridge will be placed in a gear, an appropriate node is selected (using the Platform attribute of the cartridge). The desired changes to origin should be similar to what we have implemented in the PoC, as described below (we will have to use the new 'Platform' attribute instead of looking for a 'windows' category).

Modified files (in the PoC):

- [`./origin-server/controller/app/models/application.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/app/models/application.rb)
	
	Modified the `calculate_ops` method to look and see if the cartridge to be deployed is a Windows cartridge, and to pass the needed kernel to the creation of the new group instance.  

- [`./origin-server/controller/app/models/gear.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/app/models/gear.rb) 

    Modified the `reserve_uuid` method - it uses the 'kernel' of the group instance that will be deployed on the new gear to find a node.

- [`./origin-server/controller/app/models/group_instance.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/app/models/group_instance.rb)
 
    Added a new field to the group instance model, called 'kernel'. Its default value is 'Linux'.

- [`./origin-server/controller/app/models/pending_ops/create_group_instance_op.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/app/models/pending_ops/create_group_instance_op.rb)

	Added a 'kernel' field and changed the creation of the GroupInstance object to include the 'kernel'. 

## Creating applications with a windows web cartridge

If an application is created using a Windows Web Cartridge, it will automatically be created as a scalable app.

An error will be thrown if a Windows Addon Cartridge is added to an application that has a Linux Web Cartridge and the app is not scalable.

Modified files (in the PoC):

- [`./origin-server/controller/app/models/application.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/app/models/application.rb)
	
	Added a bunch of conditions to existing if statements, essentially treating Windows apps as Scalable apps, even if they aren't Scalable

## Build flow
1. The HAProxy cartridge will have to run remote commands to a designated Windows gear – we will designate a gear for predictability (we could sort gears by their UUIDs and pick the first)
2. The HAProxy will host the git server, even for a Windows app – we will have to make sure that this setup won’t mess up line endings 
3. Using the build process described [here](https://github.com/openshift/openshift-pep/blob/master/openshift-pep-006-deploy.md#git-deployments---preserving-previous-deployments) as a starting point, the following points will have to be changed:
 - On number 7, the contents of the git repository will be uploaded to the designated gear and unpacked there
 - On number 8, the HAProxy cartridge will run the commands on the designated gear via ssh (this should work out of the box, since authorized keys are shared on the Windows gears just like on the Linux gears)
 - On number 9, just like number 8, the commands will be run remotely via ssh; when this is complete, the build artifacts will be downloaded from the designated gear to the HAProxy gear via scp

# Notes

It doesn't seem to make sense to support Windows builds on a Linux Jenkins cartridge.
The Linux Jenkins cartridge will not be able to support building/testing Windows source code (C/C++/.Net/etc.).
We will have to build a Jenkins cartridge for Windows.  
