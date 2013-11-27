If (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(`
    [Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Host "This script requires elevation. Please run as administrator." -ForegroundColor Red
    exit 1
}

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\..\common\openshift-common.psm1') -DisableNameChecking

Import-NodeLib
Import-CommonLib

Write-Host "Loading configuration values ..."

$config = [Uhuru.Openshift.Runtime.Config.NodeConfig]::Values

Write-Host "Looking up network settings ..."

# Network configuration objects
$externalAdapter = get-wmiobject -class "Win32_NetworkAdapter" | Where { $_.netConnectionId -match $config["EXTERNAL_ETH_DEV"] }
$internalAdapter = get-wmiobject -class "Win32_NetworkAdapter" | Where { $_.netConnectionId -match $config["INTERNAL_ETH_DEV"] }
$externalAdapterConfiguration = get-wmiobject -class "Win32_NetworkAdapterConfiguration" | Where { $_.Index -eq $externalAdapter.InterfaceIndex }
$internalAdapterConfiguration = get-wmiobject -class "Win32_NetworkAdapterConfiguration" | Where { $_.Index -eq $internalAdapter.InterfaceIndex }

Write-Host "Loading memory information ..."

# Memory Info
$totalMemoryMB = (Get-WmiObject -Class Win32_ComputerSystem).TotalPhysicalMemory / 1048576
$totalSwapMB = (Get-WmiObject -Class Win32_PageFileUsage).AllocatedBaseSize
$freeMemoryMB = (Get-WmiObject -Class Win32_OperatingSystem).FreePhysicalMemory / 1048576
$freeSwapMB = (Get-WmiObject -Class Win32_PageFileUsage).AllocatedBaseSize - (Get-WmiObject -Class Win32_PageFileUsage).CurrentUsage

Write-Host "Creating facts dictionary ..."

$facts = @{
    architecture  = "x86_64"
    kernel = "Windows"
    domain = $config["CLOUD_DOMAIN"]
    macaddress = $internalAdapterConfiguration.macAddress.ToString()
    operatingsystem = $(Get-WmiObject Win32_OperatingSystem).Caption.ToString()
    fqdn = $config["PUBLIC_HOSTNAME"]
    hostname = $(hostname).ToString()
    ipaddress = ($internalAdapterConfiguration.IpAddress | where { $_ -notmatch ':' }).ToString()
    kernelmajversion = (Get-WmiObject Win32_OperatingSystem).version.ToString()
    memorysize = "$totalMemoryMB MB"
    memoryfree = "$freeMemoryMB MB"
    swapsize = "$totalSwapMB MB"
    swapfree = "$freeSwapMB MB"
    memorytotal = "$totalMemoryMB MB"
    district_uuid = "NONE"
    district_active = $false
    public_ip = $($externalAdapterConfiguration.IpAddress | where { $_ -notmatch ':' }).ToString()
    public_hostname = $config["PUBLIC_HOSTNAME"]
    node_profile = "small"
    max_active_gears = 100
    quota_blocks = 409600
    quota_files = 50000
    gears_active_count = 0
    gears_total_count = 0
    gears_idle_count = 0
    gears_stopped_count = 0
    gears_started_count = 0
    gears_deploying_count = 0
    gears_unknown_count = 0
    gears_usage_pct = 0.0
    gears_active_usage_pct = 0.0
    git_repos = 0
    capacity = "0.0"
    active_capacity = "0.0"
    cart_list = "uhuru-dotnet-4.5"
    embed_cart_list = ""
    mcollective = 1  
}

Write-Host "Writing facts to 'c:\mcollective\etc\facts.yaml' ..."

$factCreator = New-Object Uhuru.Openshift.Common.FactCreator($facts)

echo $factCreator.GetYaml() | Out-File "c:\mcollective\etc\facts.yaml" -Encoding ascii

