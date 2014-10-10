[CmdletBinding()]
param (
    [string] $installLocation = 'c:\openshift\mcollective\',
    [string] $cygwinInstallLocation = 'c:\openshift\cygwin\installation\',
    [string] $localGemsDir = [string]::Empty
)

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\..\common\openshift-common.psd1') -DisableNameChecking

If (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(`
    [Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Error "This script requires elevation. Please run as administator."
    exit 1
}

Write-Host "Silently setting up mcollective ..."

$rubyInterpreter = (Get-Command "ruby" -ErrorAction SilentlyContinue | select path).Path


if ([string]::IsNullOrEmpty($rubyInterpreter))
{
    Write-Error "Could not find ruby. Please install it before setting up mcollective."
    exit 1
}

Write-Host "Found ruby here: " -NoNewline
Write-Host $rubyInterpreter -ForegroundColor Yellow

$rubyVersion = [string](ruby -v)

if (($rubyVersion -notlike "*1.8.7*") -and ($rubyVersion -notlike "*1.9.3*"))
{
    Write-Error "Incorrect ruby version: $rubyVersion. Ruby 1.8.7 or 1.9.3 required."
    exit 1
}

Write-Host "Detected ruby version: " -NoNewline
Write-Host $rubyVersion -ForegroundColor Yellow

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent

$mcollectiveSetupURL = "http://downloads.puppetlabs.com/mcollective/mcollective-2.3.3.tar.gz"

Write-Host "Downloading mcollective setup package from here: " -NoNewline
Write-Host $mcollectiveSetupURL -ForegroundColor Yellow

$setupPackage = [System.IO.Path]::GetTempFileName() + '.tar.gz'

if ((Test-Path $setupPackage) -eq $true)
{
    rm $setupPackage -Force > $null
}

if ([string]::IsNullOrWhiteSpace($env:osiProxy))
{
    Invoke-WebRequest $mcollectiveSetupURL -OutFile $setupPackage
}
else
{
    Invoke-WebRequest $mcollectiveSetupURL -OutFile $setupPackage -Proxy $env:osiProxy
}

Write-Verbose 'Looking up binaries from cygwin ...'

$bash = (Join-Path $cygwinInstallLocation 'bin\bash.exe')
$cygpath = (Join-Path $cygwinInstallLocation 'bin\cygpath.exe')
if ((Test-Path $bash) -ne $true)
{
    Write-Error "Can't find the bash binary in the cygwin installation path."
    exit 1
}
if ((Test-Path $cygpath) -ne $true)
{
    Write-Error "Can't find the cygpath binary in the cygwin installation path."
    exit 1
}

if (Test-Path $installLocation)
{
    Write-Verbose "Cleaning up existing directory '$installLocation'"
    Remove-Item -Force -Recurse -Path $installLocation
}

Write-Verbose "Creating directory '${installLocation}' ..."
New-Item -path $installLocation -type directory -Force | Out-Null

Write-Host 'Unpacking mcollective ...'
$setupPackageCyg = & $cygpath $setupPackage
$installLocationCyg = & $cygpath $installLocation
Write-Verbose "Using the following command: '${bash} --login --norc -c ""tar zxf ${setupPackageCyg} -C ${installLocationCyg}"""

$unpackProcess = Start-Process -Wait -PassThru -NoNewWindow -WorkingDirectory (Join-Path $cygwinInstallLocation 'bin') $bash "--norc --login -c ""tar zxf ${setupPackageCyg} -C ${installLocationCyg}"""

if ($unpackProcess.ExitCode -ne 0)
{
    Write-Error "Error unpacking mcollective. Aborting installation."
    exit 1
}

Write-Verbose "MCollective was unpacked successfully."

$mcollectiveUnpackDir = Get-ChildItem -Path "C:\openshift\mcollective\mcollective-*"

Copy-Item -Force -Recurse -Path (Join-Path $mcollectiveUnpackDir '\*') $installLocation
Remove-Item -Force -Recurse -Path $mcollectiveUnpackDir

Write-Host "Setting up mcollective gem dependencies ..."

if ($localGemsDir -eq [string]::Empty)
{
    $gemInstallProcess = Start-Process -Wait -PassThru -NoNewWindow "gem" "install sys-admin win32-process win32-dir win32-service stomp windows-pr win32-security facter"
}
else
{
    $gemInstallProcess = Start-Process -WorkingDirectory $localGemsDir -Wait -PassThru -NoNewWindow "gem" "install *.gem"
}

if ($gemInstallProcess.ExitCode -ne 0)
{
    Write-Error "Error setting up gems for mcollective."
    exit 1
}
