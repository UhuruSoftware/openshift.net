$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking

$helpCommands = "help", "--help", "-help", "/?", "-?"

if ($helpCommands -contains $args[0])
{
    Get-Help OO-Idler-Stats
}
else
{
    $output = OO-Idler-Stats
    write-host $output
}
exit 0