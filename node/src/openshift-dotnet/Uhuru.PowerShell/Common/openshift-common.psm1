Set-StrictMode -Version 3

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent

. (Join-Path $currentDir "template-mechanism.ps1")
. (Join-Path $currentDir "cygwin-passwd.ps1")
. (Join-Path $currentDir "file-ownership.ps1")
. (Join-Path $currentDir "library-importer.ps1")

function Get-NotEmpty($a, $b) 
{ 
    if ([string]::IsNullOrWhiteSpace($a)) 
    { 
        $b 
    } else 
    { 
        $a 
    }
}
