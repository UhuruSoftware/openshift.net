## Windows Extensions for OpenShift 0.1 Release Notes ##

### Additions ###

- administrator user has access to oo-* commands

### Changes ###

- prison features activated: cpu, memory, 
- prison restriction values come from node profile file
- node profile comes from file
- district info comes from dir

### Known Issues###

- Upgrade will not work from previous version
- Gears will not be gracefully stopped on system shutdown

## Windows Extensions for OpenShift 0.1 Release Notes ##

### Additions ###

- support for Windows Nodes in an OpenShift deployment
- fully automated installation package for the Windows Node
- the 'DotNet 4.5' web cartridge that provides support .NET web applications
	- any version of .NET is supported - 2.0 - 4.5, x86 and x64
	- applications are hosted using the IIS Hostable Web Core
	- the application has full control the applicationhost.config and root web.config files
- the 'MS SQL Server 2008' addon cartridge that provides access to SQL Server databases
	- the user gets the 'sa' account to the server
	- an empty database is automatically created
	- connection information is provided via environment variables
- applications can contain mixed cartridges (Windows and Linux)
	- deploying .NET applications that use Linux services is possible
	- configuration happens using environment variables
- users can deploy their Windows code like they do for Linux apps
	- they can push their code via git using the remote given to them by rhc
	- they can use a template git url
- Windows applications are secured using the Uhuru Prison
	- applications run in the context of unprivileged users
	- memory quotas are enforced via Job Objects
	- HTTP Server API bindings are restricted
	- applications run in separate window stations

### Changes ###

- cartridges from both environments are available to the user
- applications that have cartridges deployed on Windows Nodes must be scalable
- Windows web cartridges always sit behind HAProxy load balancer

### Known Issues###

- port forward to a SQL Server cartridge does not work
- the Uhuru Prison is not fully locked (CPU and network restrictions, disk ACLs and disk quotas are not enforced)
- jenkins builds will not work with Windows apps
- gear artifacts are not cleaned 100%
- concurrency - the Windows Nodes might be unpredictable if used by multiple users at the same time
- non-scalable Windows applications are allowed, although they cannot be accessed via port 80
- district support is not available for Windows Nodes
- the admin console does not work with Windows gears
- oo-* administration scripts are not available via SSH to the administrator Windows user
- Windows node profiles cannot be changed