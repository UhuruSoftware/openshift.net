$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking

$json = ConvertFrom-Json -InputObject $args[0]

$status = OO-Authorized-Ssh-Keys-Replace -WithAppUuid $json.'--with-app-uuid' -WithAppName $json.'--with-app-name' -WithContainerUuid $json.'--with-container-uuid' -WithContainerName $json.'--with-container-name' -WithNamespace $json.'--with-namespace' -WithRequestId $json.'--with-request-id' -WithSshKeys $json.'--with-ssh-keys'
write-host $status.Output
exit $status.ExitCode