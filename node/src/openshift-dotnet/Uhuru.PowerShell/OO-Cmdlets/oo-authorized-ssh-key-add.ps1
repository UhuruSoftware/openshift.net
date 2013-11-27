$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
$modulePath = Join-Path $scriptPath '..\..\..\bin\Uhuru.Openshift.Cmdlets.dll'
Import-Module $modulePath -DisableNameChecking
$json = ConvertFrom-Json -InputObject $args[0]
write-host $args
$output = OO-Authorized-Ssh-Key-Add -WithAppUuid $json.'--with-app-uuid' -WithAppName $json.'--with-app-name' -WithContainerUuid $json.'--with-container-uuid' -WithContainerName $json.'--with-container-name' -WithNamespace $json.'--with-namespace' -WithRequestId $json.'--with-request-id' -WithSshKey $json.'--with-ssh-key' -WithSshKeyComment $json.'--with-ssh-key-comment' -WithSshKeyType $json.'--with-ssh-key-type'
write-host $output
exit 0