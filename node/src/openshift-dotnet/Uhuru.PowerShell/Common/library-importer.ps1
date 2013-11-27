
$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent

function Import-CommandletsLib {
    $dllPath = Join-Path $currentDir "..\..\Uhuru.Openshift.Cmdlets.dll"
    Import-Module $dllPath -DisableNameChecking
}

function Import-CommonLib {
    $dllPath = Join-Path $currentDir "..\..\Uhuru.Openshift.Common.dll"
    Import-Module $dllPath -DisableNameChecking
}

function Import-NodeLib {
    $dllPath = Join-Path $currentDir "..\..\Uhuru.Openshift.Node.dll"
    Import-Module $dllPath -DisableNameChecking
}
