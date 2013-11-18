$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
$modulePath = Join-Path $scriptPath '..\..\..\bin\Uhuru.Openshift.Cmdlets.dll'
Import-Module $modulePath -DisableNameChecking
$json = ConvertFrom-Json -InputObject $args[0]

$output = OO-Configure -WithAppUuid $json.'--with-app-uuid' -WithAppName $json.'--with-app-name' -WithContainerUuid $json.'--with-container-uuid' -WithContainerName $json.'--with-container-name' -WithNamespace $json.'--with-namespace' -WithRequestId $json.'--with-request-id' -CartName $json.'--cart-name' -ComponentName $json.'--component-name' -WithSoftwareVersion $json.'--with-software-version' -CartridgeVendor $json.'--cartridge-vendor'
write-host $output
exit 0