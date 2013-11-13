If (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(`
    [Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Host "This script requires elevation. Please run as administator." -ForegroundColor Red
    exit 1
}

Write-Host "Silently setting up mcollective ..."

$rubyInterpreter = (Get-Command "ruby" -ErrorAction SilentlyContinue | select path).Path


if ([string]::IsNullOrEmpty($rubyInterpreter))
{
    Write-Host "Could not find ruby. Please install it before setting up mcollective." -ForegroundColor Red
    exit 1
}

Write-Host "Found ruby here: " -NoNewline
Write-Host $rubyInterpreter -ForegroundColor Yellow

$rubyVersion = [string](ruby -v)

if (($rubyVersion -notlike "*1.8.7*") -and ($rubyVersion -notlike "*1.9.3*"))
{
    Write-Host "Incorrect ruby version: $rubyVersion. Ruby 1.8.7 or 1.9.3 required." -ForegroundColor Red
    exit 1
}

Write-Host "Detected ruby version: " -NoNewline
Write-Host $rubyVersion -ForegroundColor Yellow


$devKitPath = $ENV:RI_DEVKIT

if ([string]::IsNullOrEmpty($devKitPath))
{
    Write-Host "Can't find the ruby devkit. Please make sure it's installed and environment variables are set." -ForegroundColor Red
    exit 1
}

Write-Host "Using ruby devkit from here: " -NoNewline
Write-Host $devKitPath -ForegroundColor Yellow

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent

Write-Host "Looking for mcollective setup package here: " -NoNewline
Write-Host $currentDir -ForegroundColor Yellow

$setupPackage = Get-ChildItem "$currentDir/mcollective*setup.exe"


if ($setupPackage.count -eq 0)
{
    Write-Host "No mcollective setup package was found. Aborting." -ForegroundColor Red
    exit 1
}

Write-Host "Found package " -NoNewline
Write-Host $setupPackage -ForegroundColor Yellow

Write-Host "Executing installation package ..."

$logFile = "$currentDir\mcollective_setup.log"

$arguments = "/SP /VERYSILENT /SUPRESSMSGBOXES /LOG=`"$logFile`" /NOCANCEL /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS /DIR=c:\mcollective /NOICONS"


Write-Host "Using the following arguments: " -NoNewline
Write-Host $arguments -ForegroundColor Yellow

Start-Process $setupPackage $arguments -Wait

Write-Host "Completed. You can find the log of the installation here: " -ForegroundColor Green -NoNewline
Write-Host $logFile -ForegroundColor Yellow