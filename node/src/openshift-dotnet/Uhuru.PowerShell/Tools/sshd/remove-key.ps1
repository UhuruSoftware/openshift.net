param (
    $targetDirectory = $( Read-Host "Path to target sshd installation dir (c:\cygwin\installation\)" ),
    $windowsUser = $( Read-Host "Windows User (administrator)" ),
    $key = $( Read-Host "Public key - required" )
)

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\..\common\openshift-common.psd1') -DisableNameChecking

$targetDirectory = Get-NotEmpty $targetDirectory "c:\cygwin\installation\"
$windowsUser = Get-NotEmpty $windowsUser "administrator"

if ([string]::IsNullOrEmpty($key))
{
    Write-Host "Key was not provided. Aborting." -ForegroundColor Red
    exit 1
}


$userInfo = Get-SSHDUser $targetDirectory $windowsUser

$cygpathBinary = Join-Path $targetDirectory 'bin\cygpath.exe'

$homeDir = & $cygpathBinary -w $userInfo['home']

$authorizedKeysFile = Join-Path $homeDir ".ssh\authorized_keys"

$content = Get-Content $authorizedKeysFile -Encoding Ascii
$content = $content | Where {$_ -ne $key}
$content | Out-File $authorizedKeysFile -Force -Encoding Ascii

Write-Host "Done" -ForegroundColor Green