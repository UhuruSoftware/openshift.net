$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking

$json = ConvertFrom-Json -InputObject $args[0]

$output = OO-Authorized-Ssh-Key-Add -WithAppUuid $json.'--with-app-uuid' -WithAppName $json.'--with-app-name' -WithContainerUuid $json.'--with-container-uuid' -WithContainerName $json.'--with-container-name' -WithNamespace $json.'--with-namespace' -WithRequestId $json.'--with-request-id' -WithSshKey $json.'--with-ssh-key' -WithSshKeyComment $json.'--with-ssh-key-comment' -WithSshKeyType $json.'--with-ssh-key-type'
write-Output $output.Output
exit $output.ExitCode