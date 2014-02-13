$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking

$helpCommands = "help", "--help", "-help", "/?", "-?"

if ($helpCommands -contains $args[0])
{
    Get-Help OO-Accept-Node
}
else
{
    $argsString = [string]::Join(" ", $args)
    $output = Invoke-Expression "OO-Accept-Node $argsString"
	if($output.ExitCode -eq 0)
	{
		write-Output "PASS"
	}
	else
	{
		$ReportErrorShowSource = $false
		[Console]::Error.WriteLine($output.ExitCode.ToString() + " ERRORS")
	}
    exit $output.ExitCode
}

exit 0