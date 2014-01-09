param (
    $targetDirectory = $( Read-Host "Path to target sshd installation dir (c:\cygwin\installation\)" ),
    $user = $( Read-Host "Username that will have access to the server (administrator)" ),
    $windowsUser = $( Read-Host "Corresponding Windows user (administrator)" ),
    $userHomeDir = $( Read-Host "User home directory (c:\cygwin\administrator_home)" ),
    $userShell = $( Read-Host "User's shell (/bin/bash)" )
)

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\..\common\openshift-common.psd1') -DisableNameChecking

$targetDirectory = Get-NotEmpty $targetDirectory "c:\cygwin\installation\"
$user = Get-NotEmpty $user "administrator"
$windowsUser = Get-NotEmpty $windowsUser "administrator"
$userHomeDir = Get-NotEmpty $userHomeDir "c:\cygwin\administrator_home"
$userShell = Get-NotEmpty $userShell "/bin/bash"

Write-Host 'Using installation dir: ' -NoNewline
Write-Host $targetDirectory -ForegroundColor Yellow

$sshdBinary = Join-Path $targetDirectory 'usr\sbin\sshd.exe'
$cygpathBinary = Join-Path $targetDirectory 'bin\cygpath.exe'
$passwdFile = Join-Path $targetDirectory 'etc\passwd'

if (((Test-Path $sshdBinary) -ne $true) -or ((Test-Path $cygpathBinary) -ne $true))
{
   Write-Host "Could not find necessary binaries in '$targetDirectory'. Aborting." -ForegroundColor Red
   exit 1
}

try
{
    $objUser = New-Object System.Security.Principal.NTAccount($windowsUser)
    $strSID = $objUser.Translate([System.Security.Principal.SecurityIdentifier])
    $userSID = $strSID.Value
}
catch
{
    Write-Host "Could not get SID for user '$windowsUser'. Aborting." -ForegroundColor Red
    exit 1
}

$usersGroupSID = Get-NoneGroupSID

Write-Host "Creating user home directory ..."
mkdir -Path $userHomeDir -ErrorAction SilentlyContinue > $null

Write-Host "Setting up user in passwd file ..."
$uid = $userSID.Split('-')[-1]
$gid = $usersGroupSID.Split('-')[-1]
$userHomeDirLinux = & $cygpathBinary $userHomeDir

$userShell = & $cygpathBinary $userShell

$content = Get-Content $passwdFile 
$userEntry = "${user}:unused:${uid}:${gid}:${windowsUser},${userSID}:${userHomeDirLinux}:${userShell}"
$content = $content | Where {$_ -ne $userEntry}
$content | Out-File $passwdFile -Force -Encoding Ascii