$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking

$helpCommands = "help", "--help", "-help", "/?", "-?"

if ($helpCommands -contains $args[0])
{
    Get-Help OO-Admin-Ctl-Gears
}
else
{
    $output = & OO-Admin-Ctl-Gears $args
    write-Output $output.Output
	exit $output.ExitCode
}

exit 0