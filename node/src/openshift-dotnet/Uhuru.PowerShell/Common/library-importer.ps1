
$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent

Import-Module (Join-Path $currentDir "..\..\Uhuru.Openshift.Cmdlets.dll") -DisableNameChecking

Import-Module (Join-Path $currentDir "..\..\Uhuru.Openshift.Common.dll") -DisableNameChecking

Import-Module (Join-Path $currentDir "..\..\Uhuru.Openshift.Node.dll") -DisableNameChecking
