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

In order to be able to tell what cartridges are supported on which nodes, a new fact named "Platform" will be added for both Windows and Linux nodes. This will require code changes to origin (specifically the part that generates the facts.yaml file for nodes). The fact itself should be 'linux' for Redhat and Fedora, and 'windows' for Windows. 

These facts will be generated based on a 'NODE_PLATFORM' configuration setting in node.conf, both on Windows and Linux.

### Cartridges ###

In order to properly understand the OS requirements for a cartridge, we will add a new top-level attribute in the cartridge manifest file. The attribute will be an array of strings named "Platform". Each of the strings in the array will represent an OS that is compatible with the cartridge. The string will have to match the Platform fact exposed by OpenShift nodes. 

Examples:

**Windows cartridge:**

	Name: dotnet
	Platform: ['windows']

**Linux cartridge:**

	Name: ruby
	Platform: ['linux']

Platform matching should be case insensitive.

### Broker Code Changes

The broker needs to be aware of what platforms are available in the network. Brokers will be configured with a list of platforms in the broker.conf file (configuration name CLOUD_PLATFORMS with a comma separated list of platforms). This is required when listing cartridges.

The MCollective proxy classes also have to be modified to accept filtering using the 'Platform' fact. 

The broker has to be modified so it does not allow moving gears between Linux and Windows nodes.

Example:

	# Domain suffix to use for applications (Must match node config)
	CLOUD_DOMAIN="openshift.local"
	# Comma seperted list of valid gear sizes
	VALID_GEAR_SIZES="small,medium"

	# Comma separated list of available node platforms 
	CLOUD_PLATFORMS="windows,linux"	
	...
 
Modified files (in the PoC):

- [`./origin-server/controller/lib/openshift/application_container_proxy.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/lib/openshift/application_container_proxy.rb)

	Modified the `find_available` and `find_one` methods to accept a kernel parameter.

- [`./origin-server/plugins/msg-broker/mcollective/lib/openshift/mcollective_application_container_proxy.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/plugins/msg-broker/mcollective/lib/openshift/mcollective_application_container_proxy.rb)

	Modified the `find_available_impl` and `find_one_impl` methods to accept a kernel parameter. Modified the `rpc_find_available` and `rpc_find_one` methods to filter nodes based on the 'kernel' fact.

## Listing cartridges

The broker will use a list of available platforms and query a node for each type of platform - this will compile a complete list of all available cartridges.  

Modified files (in the PoC):

- [`./origin-server/controller/app/helpers/cartridge_cache.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/app/helpers/cartridge_cache.rb)

	Modified the `get_all_cartridges` method to grab cartridges for a Linux node and then a Windows node, and then merge the results.  

## Creating Gears

Based on what type of cartridge will be placed in a gear, an appropriate node is selected (using the Platform attribute of the cartridge). The desired changes to origin should be similar to what we have implemented in the PoC, as described below (we will have to use the new 'Platform' attribute instead of looking for a 'windows' category). The entity that is currently (in the PoC) tied to the Platform attribute is the 'GroupInstance'. Even though group instances can contain multiple gears, no group instance will be able to contain gears with different platforms.  

Modified files (in the PoC):

- [`./origin-server/controller/app/models/application.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/app/models/application.rb)
	
	Modified the `calculate_ops` method to look and see if the cartridge to be deployed is a Windows cartridge, and to pass the needed kernel to the creation of the new group instance.  

- [`./origin-server/controller/app/models/gear.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/app/models/gear.rb) 

    Modified the `reserve_uuid` method - it uses the 'kernel' of the group instance that will be deployed on the new gear to find a node.

- [`./origin-server/controller/app/models/group_instance.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/app/models/group_instance.rb)
 
    Added a new field to the group instance model, called 'kernel'. Its default value is 'Linux'.

- [`./origin-server/controller/app/models/pending_ops/init_gear_op.rb`](https://github.com/UhuruSoftware/origin-server/blob/master/controller/app/models/pending_ops/init_gear_op.rb)

	Added a 'kernel' field and changed the creation of the GroupInstance object to include the 'kernel'. 

## Creating applications with a windows web cartridge

Applications that involve Windows will always need to be marked as scalable. The user will be informed by the broker via an error message if his application is not consistent with the scalable flag.

<table>
    <tr><td><strong>Web Cartridge</strong></td><td><strong>Addon Cartrige</strong></td><td><strong>Scalable</strong></td></tr>
    <tr><td>Windows</td><td>None</td><td>Required</td></tr>
    <tr><td>Windows</td><td>Linux</td><td>Required</td></tr>
    <tr><td>Windows</td><td>Windows</td><td>Required</td></tr>
    <tr><td>Linux</td><td>None</td><td>Optional</td></tr>
    <tr><td>Linux</td><td>Linux</td><td>Optional</td></tr>
    <tr><td>Linux</td><td>Windows</td><td>Required</td></tr>
</table>

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

# Jenkins

The Jenkins server cartridge will be hosted on Linux for Windows apps as well. When a Windows application has to be built the Jenkins server will use a Windows Jenkins slave for the build. This means that the Jenkins Server will have to be aware of the Platform of the application's web cartridge and create slaves accordingly. 

We assume that the Jenkins client cartridge will be compatible with Windows.