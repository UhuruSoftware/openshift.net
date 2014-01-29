$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking
 
$status = Invoke-Expression "Get-All-Gears-Endpoints-Action"
write-Output $status.Output
exit $status.ExistCode