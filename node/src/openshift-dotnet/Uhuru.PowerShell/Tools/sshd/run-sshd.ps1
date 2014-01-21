param (
    $targetDirectory = $( Read-Host "Path to cygwin installation dir (c:\cygwin\installation)" )
)

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\..\common\openshift-common.psd1') -DisableNameChecking

$targetDirectory = Get-NotEmpty $targetDirectory "c:\cygwin\installation"

$bashBinary = Join-Path $targetDirectory 'bin\bash.exe'

$args = "--norc --login -c '/usr/sbin/sshd -D -f /etc/sshd_config'"

Start-Process $bashBinary $args -NoNewWindow