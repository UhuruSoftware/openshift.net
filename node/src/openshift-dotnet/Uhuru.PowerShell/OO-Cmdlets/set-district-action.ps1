$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking

$arguments = $args -replace """", ''
$status = Invoke-Expression "Set-District-Action $arguments"
write-Output $status.Output
exit $status.ExistCode