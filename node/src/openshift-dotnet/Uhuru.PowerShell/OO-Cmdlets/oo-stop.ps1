$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking

$json = ConvertFrom-Json -InputObject $args[0]

$output = OO-Stop -WithAppUuid $json.'--with-app-uuid' -WithAppName $json.'--with-app-name' -WithContainerUuid $json.'--with-container-uuid' -WithContainerName $json.'--with-container-name' -WithNamespace $json.'--with-namespace' -WithRequestId $json.'--with-request-id' -CartName $json.'--cart-name' -ComponentName $json.'--component-name' -WithSoftwareVersion $json.'--with-software-version' -CartridgeVendor $json.'--cartridge-vendor'
write-Output $output.Output
exit $output.ExitCode