$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking

$parameters = [string]::Join(' ', $args)
Invoke-Expression "OO-Gear $parameters"

exit 0