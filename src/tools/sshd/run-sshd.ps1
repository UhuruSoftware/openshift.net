param (
    $targetDirectory = $( Read-Host "Path to target sshd installation dir (c:\cygwin\installation\)" ),
    $user = $( Read-Host "User who's configuration will be loaded (administrator)" )
)

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\..\powershell_common\openshift-common.psm1')

$targetDirectory = Get-NotEmpty $targetDirectory "c:\cygwin\installation\"
$user = Get-NotEmpty $user "administrator"


$bashBinary = Join-Path $targetDirectory 'bin\bash.exe'

$args = "--norc --login -c '/usr/sbin/sshd -d -f /cygdrive/c/cygwin/administrator_home/.sshd_etc/sshd_config'"

Start-Process $bashBinary $args -Wait -NoNewWindow