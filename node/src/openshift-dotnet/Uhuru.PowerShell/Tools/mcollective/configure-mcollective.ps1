param (
    $userActivemqServer = $(Read-Host "ActiveMQ Host (OpenShift Broker - required)"),
    $userActivemqPort = $(Read-Host "ActiveMQ Port (default is 61613)"),
    $userActivemqUser = $(Read-Host "ActiveMQ Username (default is mcollective)"),
    $userActivemqPassword = $(Read-Host "ActiveMQ Password (default is marionette)"),
    $binDir = $(Read-Host "Binary directory (required)")
    )

If (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(`
    [Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Host "This script requires elevation. Please run as administrator." -ForegroundColor Red
    exit 1
}

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\..\common\openshift-common.psd1') -DisableNameChecking

Import-Module (Get-NodeLib)

$userActivemqPort = Get-NotEmpty $userActivemqPort "61613"
$userActivemqUser = Get-NotEmpty $userActivemqUser "mcollective"
$userActivemqPassword = Get-NotEmpty $userActivemqPassword "marionette"

if ([string]::IsNullOrEmpty($userActivemqServer))
{
    Write-Host "Broker host is empty - please specify a valid hotname or IP. Aborting." -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrEmpty($binDir))
{
    Write-Host "Binary directory is empty - please specify the correct path to the OpenShift.NET binaries. Aborting." -ForegroundColor Red
    exit 1
}

Write-Host "This script will configure a local mcollective service.`n" -ForegroundColor Cyan

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent

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

$agentDDLFile = Join-Path $binDir "mcollective\openshift.ddl"
$agentCodeFile = Join-Path $binDir "mcollective\openshift.rb"

# validate the ddl and the rb files are there
if ((Test-Path $agentDDLFile) -ne $true)
{
    Write-Host "Could not find $agentDDLFile. Aborting." -ForegroundColor Red
    exit 1
}

if ((Test-Path $agentCodeFile) -ne $true)
{
    Write-Host "Could not find $agentCodeFile. Aborting." -ForegroundColor Red
    exit 1
}

Write-Host "Setting up OpenShift development agent ..."
Write-Host "Warning - The DDL file will be copied, not included. If you change the DDL file, run this script again." -ForegroundColor Yellow

# copy ddl file
Copy-Item $agentDDLFile "C:\mcollective\plugins\mcollective\agent\" -Force

# create an agent that includes the development agent

Write-Template (Join-Path $currentDir "openshift.rb.template") "C:\mcollective\plugins\mcollective\agent\openshift.rb" @{
    devAgentCodeFile = $agentCodeFile
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

Write-Template (Join-Path $currentDir "client.cfg.template")  "c:\mcollective\etc\client.cfg" @{
    activemqServer = $userActivemqServer
    activemqPort = $userActivemqPort
    activemqUser = $userActivemqUser
    activemqPassword = $userActivemqPassword
}

# edit c:\mcollective\etc\server.cfg
Write-Host "Configuring c:\mcollective\etc\server.cfg"

Write-Template (Join-Path $currentDir "server.cfg.template")  "c:\mcollective\etc\server.cfg" @{
    activemqServer = $userActivemqServer
    activemqPort = $userActivemqPort
    activemqUser = $userActivemqUser
    activemqPassword = $userActivemqPassword
    binDir = $binDir
} 

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

$nodeConfPath = [Uhuru.Openshift.Runtime.Config.NodeConfig]::NodeConfigFile

if (Test-Path $nodeConfPath)
{
    Write-Host "Found configuration file here: '${nodeConfPath}' - setting up facts file."
    & $( Join-Path $currentDir "update-facts.ps1" )
}
else
{
    Write-Host "Did not find configuration file '${nodeConfPath}' - skipping writing facts file!"
}

Write-Host "Starting mcollectived ..."
$mcollectiveService.Start()