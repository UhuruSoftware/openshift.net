If (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(`
    [Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Host "This script requires elevation. Please run as administrator." -ForegroundColor Red
    exit 1
}

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\..\common\openshift-common.psd1') -DisableNameChecking

$factsFile = 'c:\openshift\mcollective\etc\facts.yaml'

# TODO: vladi: Implement global lock

Write-Host "Writing facts to '${factsFile}' ..."

[Uhuru.Openshift.Runtime.Utils.Facter]::WriteFactsFile($factsFile);

