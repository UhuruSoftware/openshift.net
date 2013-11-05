This folder contains cartridge scripts.
The scripts will be run directly from the home directory of the cartridge. They need to have the ps1 extension. 

## Mandatory scripts ##
- setup.ps1 - Prepare this instance of cartridge to be operational for the initial install and each incompatible upgrade
- control.ps1 - Command cartridge to report or change state

## Optional scripts ##
- teardown.ps1 - Prepare this instance of cartridge to be removed
- install.ps1 - Prepare this instance of cartridge to be operational for the initial install
- post-install.ps1 - An opportunity for configuration after the cartridge has been started for the initial install

## Exit status codes ##
OpenShift follows the convention that your scripts should return zero for success and non-zero for failure. Additionally, OpenShift supports special handling of the following non-zero exit codes:
- 127 - ??
- 131 - ??

These exit status codes will allow OpenShift to refine its behaviour when returning HTTP status codes for the REST API, whether an internal operation can continue or should aborted, etc. Should your script return a value not included in this table, OpenShift will assume the problem is fatal to your cartridge.


