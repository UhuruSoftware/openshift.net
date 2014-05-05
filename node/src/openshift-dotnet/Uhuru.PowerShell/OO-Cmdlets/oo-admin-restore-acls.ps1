$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking

$json = ConvertFrom-Json -InputObject $args[0]

$output = OO-Admin-Restore-Acls  -Uuid $json.'--uuid'
write-Output $output.Output
exit $output.ExitCode