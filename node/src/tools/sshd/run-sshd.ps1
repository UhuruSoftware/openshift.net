param (
    $targetDirectory = $( Read-Host "Path to cygwin installation dir (c:\cygwin\installation)" ),
    $user = $( Read-Host "Windows user who's configuration will be loaded (administrator)" )
)

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\..\powershell_common\openshift-common.psm1')

$targetDirectory = Get-NotEmpty $targetDirectory "c:\cygwin\installation"
$user = Get-NotEmpty $user "administrator"


$bashBinary = Join-Path $targetDirectory 'bin\bash.exe'

$cygwinUser = Get-SSHDUser $targetDirectory "${user}"

if ($cygwinUser -eq $null)
{
    Write-Host "Could not find user '$user' in the configuration files. Aborting." -ForegroundColor Red
    exit 1
}

$userHome = $cygwinUser['home']

$args = "--norc --login -c '/usr/sbin/sshd -D -f ${userHome}/.sshd_etc/sshd_config'"

Start-Process $bashBinary $args -Wait -NoNewWindow