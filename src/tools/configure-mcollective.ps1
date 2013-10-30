If (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(`
    [Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Host "This script requires elevation. Please run as administator." -ForegroundColor Red
    exit 1
}

Write-Host "This script will help you configure a local mcollective service, so it can connect to an Openshift Broker.`n" -ForegroundColor Cyan

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent

# Using a simple templating mechanism for configuring mcollective files (http://my.safaribooksonline.com/book/programming/microsoft-windows-powershell/9781449322694/4dot-accelerating-delivery/id545617)

function Invoke-Template {
    param(
        [string]$Path,
        [Scriptblock]$ScriptBlock
    )

    function Get-Template {
        param($TemplateFileName)

        $content = [IO.File]::ReadAllText(
            (Join-Path $Path $TemplateFileName) )
        Invoke-Expression "@`"`r`n$content`r`n`"@"
    }

    & $ScriptBlock
}

# check to see if mcollective is installed in c:\mcollective

if ((Test-Path C:\mcollective\bin\mcollectived) -ne $true)
{
    Write-Host "Couldn't find mcollective. Make sure you installed it properly." -ForegroundColor red
    exit 1
}

$mcollectiveService = Get-Service mcollectived -ErrorAction SilentlyContinue

if ($mcollectiveService -eq $null)
{
    Write-Host "The mcollective binaries are present, but the mcollectived Windows Service is not setup. Make sure the installation was successful." -ForegroundColor red
    exit 1
}

# ask the user for stuff
$userActivemqServer = Read-Host "ActiveMQ Host (OpenShift Broker)"

$userActivemqPort = Read-Host "ActiveMQ Port (default is 61613)"
if ([string]::IsNullOrEmpty($userActivemqPort))
{
    $userActivemqPort = "61613"
}

$userActivemqUser = Read-Host "ActiveMQ Username (default is mcollective)"
if ([string]::IsNullOrEmpty($userActivemqUser))
{
    $userActivemqUser = "mcollective"
}

$userActivemqPassword = Read-Host "ActiveMQ Password (default is marionette)"
if ([string]::IsNullOrEmpty($userActivemqPassword))
{
    $userActivemqPassword = "marionette"
}

# check if port is open on broker machine (sudo systemctl stop firewalld.service)

Write-Host "Trying to connect to ${userActivemqServer}:${userActivemqPort} ..."

try
{
    $c = New-Object System.Net.Sockets.TcpClient($userActivemqServer, $userActivemqPort)
    $c.Close()
}
catch [system.exception]
{
    Write-Host "Could not connect to ${userActivemqServer}:${userActivemqPort}." -ForegroundColor Red
    Write-Host "Make sure that the firewall on ${userActivemqServer} is not blocking the connection."
    Write-Host "For dev purposes, you can disable the firewall using the following command:"
    Write-Host "sudo systemctl stop firewalld.service" -ForegroundColor Yellow
    exit 1
}

Write-Host "Verified ${userActivemqServer}:${userActivemqPort} is open." -ForegroundColor Green

# edit c:\mcollective\etc\client.cfg

Write-Host "Configuring c:\mcollective\etc\client.cfg"

Invoke-Template $currentDir {
    $activemqServer = $userActivemqServer
    $activemqPort = $userActivemqPort
    $activemqUser = $userActivemqUser
    $activemqPassword = $userActivemqPassword
    Get-Template client.cfg.template
} | Out-File c:\mcollective\etc\client.cfg -Encoding ascii

# edit c:\mcollective\etc\server.cfg
Write-Host "Configuring c:\mcollective\etc\server.cfg"

Invoke-Template $currentDir {
    $activemqServer = $userActivemqServer
    $activemqPort = $userActivemqPort
    $activemqUser = $userActivemqUser
    $activemqPassword = $userActivemqPassword
    Get-Template server.cfg.template
} | Out-File c:\mcollective\etc\server.cfg -Encoding ascii

# copy custom validator to C:\mcollective\plugins\mcollective\validator
Write-Host "Copying custom validator to C:\mcollective\plugins\mcollective\validator ..."
Copy-Item (Join-Path $currentDir 'any_validator.rb') "C:\mcollective\plugins\mcollective\validator\" -Force
Copy-Item (Join-Path $currentDir 'any_validator.ddl') "C:\mcollective\plugins\mcollective\validator\" -Force

# restart mcollective service

if ($mcollectiveService.status -like 'running')
{
    Write-Host "Stopping mcollectived ..."
    $mcollectiveService.Stop()

    Write-Host "Waiting 5 seconds for mcolllective to stop ..."
    Start-Sleep -s 5
}

Write-Host "Starting mcollectived ..."
$mcollectiveService.Start()