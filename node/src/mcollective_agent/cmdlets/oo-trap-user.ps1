$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
$modulePath = Join-Path $scriptPath '..\..\..\bin\Uhuru.Openshift.Cmdlets.dll'
Import-Module $modulePath -DisableNameChecking
