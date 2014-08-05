<#
.SYNOPSIS
    OpenShift Windows Node installation script.
.DESCRIPTION
    This script installs all the components of the OpenShift Windows Node.
    It does not install prerequisites such as Internet Information Services or Microsoft Sql Server.
    Before the installation is started this script will verify that all prerequisites are present and properly installed.
.PARAMETER binLocation
    Target bin directory. This is where all the OpenShift binaries will be copied.
    
.PARAMETER publicHostname
    Public hostname of the machine. This should resolve to the public IP of this node.
    
.PARAMETER brokerHost
    Hostname of the OpenShift broker.
    
.PARAMETER cloudDomain
    The domain of the cloud (e.g. mycloud.com).

.PARAMETER externalEthDevice
    Public ethernet device.
    
.PARAMETER internalEthDevice
    Internal ethernet device.
    
.PARAMETER publicIp
    Public IP of the machine (default is 'the first IP on the public ethernet card').
    
.PARAMETER gearBaseDir
    Gear base directory. This is the where application files will live.
    
.PARAMETER gearShell
    Gear shell. This is the shell that will be run when users ssh to the gear.
    
.PARAMETER gearGecos
    Gecos information. This will be the same for all gears.
    
.PARAMETER cartridgeBasePath
    Cartridge base path. This is where cartridge files will be copied.
    
.PARAMETER platformLogFile
    Log file path. This is where the OpenShift Windows Node will log information.
    
.PARAMETER platformLogLevel
    Log level. The level of detail to use when logging information.
    
.PARAMETER containerizationPlugin
    Container used for securing OpenShift gears on Windows.

.PARAMETER rubyDownloadLocation
    Ruby 1.9.3 msi package download location. The installer will download this msi and install it.
    
.PARAMETER rubyInstallLocation
    Ruby installation location. This is where ruby will be installed on the local machine.
    
.PARAMETER mcollectiveActivemqServer
    ActiveMQ Host. This is where the ActiveMQ messaging service is installed. It is usually setup in the same place as your broker.
    
.PARAMETER mcollectiveActivemqPort
    ActiveMQ Port. The port to use when connecting to ActiveMQ.
    
.PARAMETER mcollectiveActivemqUser
    ActiveMQ Username. The default ActiveMQ username for an OpenShift installation is 'mcollective'.

.PARAMETER mcollectiveActivemqPassword
    ActiveMQ Password. The default ActiveMQ password for an ActiveMQ installation is 'marionette'.

.PARAMETER mcollectivePskPlugin
    Psk plugin used in MCollective. The value for a Fedora all-in-one VM is 'unset'. 
    For a default OpenShift Enterprise installation, the value should be 'asimplething'.

.PARAMETER sshdCygwinDir
    Location of sshd installation. This is where cygwin will be installed.

.PARAMETER sshdListenAddress
    This specifies on which interface should the SSHD service listen. By default it will listen on all interfaces.
    
.PARAMETER sshdPort
    SSHD listening port.

.PARAMETER proxy
    An http proxy to use for downloading software. By default the install script won't use a proxy.
    Use the format http://host:port.

.PARAMETER skipRuby
    This is a switch parameter that allows the user to skip downloading and installing Ruby. 
    This is useful for testing, when the caller is sure Ruby is already installed in the directory specified by the -rubyInstallLocation parameter.

.PARAMETER skipCygwin
    This is a switch parameter that allows the user to skip downloading and installing Cygwin. 
    This is useful for testing, when the caller is sure Cygwin is present in the directory specified by the -sshdCygwinDir parameter.
    Note that sshd will NOT be re-configured if you skip this step.

.PARAMETER skipMCollective
    This is a switch parameter that allows the user to skip downloading and installing MCollective.
    This is useful for testing, when the caller is sure MCollective is already present in c:\openshift\mcollective. 
    Configuration of MCollective will still happen, even if this parameter is present.

.PARAMETER skipChecks
    This is a switch parameter that allows the user to skip checking for prerequisites.
    This should only be used for debugging/development purposes.

.PARAMETER skipGlobalEnv
    This is a switch parameter that allows the user to skip setting up global environment variables and aliases.
    This is useful for testing, when the user wants to manually set these variables.

.PARAMETER skipServicesSetup
    This is a switch parameter that allows the user to skip setting up Windows Services for MCollective and SSHD.
    Skipping this step also skips creating the openshift_service user account and setting its privileges.
    This is useful in development environments, when it's not necessary to restart services (e.g. the developer only wants to update the .NET binaries)

.PARAMETER skipBinDirCleanup
    This is a switch parameter that allows the user to skip cleaning up the binary directory.
    This is useful in development environments, when persistence of logs and configurations is required.

.PARAMETER upgrade
    This is a switch parameter that allows the user to upgrade an existing deployment.
    The configuration values that are not provided are taken from the existing deployment.

.PARAMETER cygwinInstallerExe
    This allows the user to point the installation script to a cygwin installer. 
    It is useful when installing behind a firewall that doesn't allow downloading executables.

.PARAMETER rubyInstallerExe
    This allows the user to point the installation script to a ruby installer. 
    It is useful when installing behind a firewall that doesn't allow downloading executables.

.PARAMETER mcollectiveGemsDir
    This allows the user to point the installation script to a folder containing the ruby gems.
    It is useful when installing behind a firewall.

.PARAMETER noSql2008
    This installs the Windows Node without SQL Server 2008 cartridge support. 
    Note that the mssql cartridge manifest must be manually updated (C:\openshift\cartridges\mssql\metadata\manifest.yml).

.PARAMETER noSql2012
    This installs the Windows Node without SQL Server 2012 cartridge support.
    Note that the mssql cartridge manifest must be manually updated (C:\openshift\cartridges\mssql\metadata\manifest.yml).

.NOTES
    Author: Vlad Iovanov
    Date:   January 17, 2014

.EXAMPLE
.\install.ps1 -mcollectivePskPlugin unset -publicHostname winnode-001.mycloud.com -brokerHost broker.mycloud.com -cloudDomain mycloud.com 
Install the node by passing the minimum information required for a Fedora all-in-one installation. 
.EXAMPLE
.\install.ps1 -mcollectivePskPlugin unset -publicHostname winnode-001.mycloud.com -brokerHost broker.mycloud.com -cloudDomain mycloud.com -publicIP 10.2.0.104
Install the node by also passing the public IP address of the machine for a Fedora all-in-one installation.
.EXAMPLE
.\install.ps1 -mcollectivePskPlugin asimplething
Install the node for an OpenShift Enterprise deployment, passing a non-default mcollectivePskPlugin.
.EXAMPLE
.\install.ps1 -mcollectivePskPlugin asimplething -publicHostname winnode-001.mycloud.com -brokerHost broker.mycloud.com -cloudDomain mycloud.com 
Install the node for an OpenShift Enterprise deployment, passing a non-default mcollectivePskPlugin and the minimum information required.
#>

[CmdletBinding()]
param (
    [Switch] $upgrade = $false,
    # parameters used for setting up the OpenShift Windows Node binaries
    [string] $binLocation = $(if (-not $upgrade) {  'c:\openshift\bin\' }),
    # parameters used for setting ip node configuration file
    [string] $publicHostname = $(if (-not $upgrade) { Read-Host "Public hostname (FQDN) of the machine" } ),
    [string] $brokerHost = $(if (-not $upgrade) { Read-Host "Hostname of the broker (FQDN)" }),
    [string] $cloudDomain = $(if (-not $upgrade) { Read-Host "Cloud domain" }),
    [string] $externalEthDevice = $(if (-not $upgrade) {  'Ethernet' }),
    [string] $internalEthDevice = $(if (-not $upgrade) {  'Ethernet' }),
    [string] $publicIp = @((get-wmiobject -class "Win32_NetworkAdapterConfiguration" | Where { $_.Index -eq (get-wmiobject -class "Win32_NetworkAdapter" | Where { $_.netConnectionId -eq $externalEthDevice }).DeviceID }).IPAddress | where { $_ -notmatch ':' })[0],
    [string] $gearBaseDir = $(if (-not $upgrade) { 'c:\openshift\gears\' }),
    [string] $gearShell = $(if (-not $upgrade) { (Join-Path $binLocation 'oo-trap-user.exe') }),
    [string] $gearGecos = $(if (-not $upgrade) {  'OpenShift guest' }),
    [string] $cartridgeBasePath = $(if (-not $upgrade) {  'c:\openshift\cartridges\' }),
    [string] $platformLogFile = $(if (-not $upgrade) { 'c:\openshift\log\platform.log' }),
    [ValidateSet('TRACE','DEBUG','WARNING','ERROR')]
    [string] $platformLogLevel = $(if (-not $upgrade) {  'DEBUG' }),
    [string] $containerizationPlugin = $(if (-not $upgrade) {  'uhuru-prison' }),
    # parameters used for ruby installation
    [string] $rubyDownloadLocation ='http://dl.bintray.com/oneclick/rubyinstaller/rubyinstaller-1.9.3-p448.exe?direct',
    [string] $rubyInstallLocation = $(if (-not $upgrade) { 'c:\openshift\ruby\' }),
    # parameters used for mcollective setup
    [string] $mcollectiveActivemqServer = $brokerHost,
    [int] $mcollectiveActivemqPort = $(if (-not $upgrade){ 61613 }),
    [string] $mcollectiveActivemqUser = $(if (-not $upgrade) {'mcollective'}),
    [string] $mcollectiveActivemqPassword = $(if (-not $upgrade) {'marionette'}),
    [string] $mcollectivePskPlugin = $(if (-not $upgrade) { Read-Host "MCollective psk plugin (default for a Fedora all-in-one VM is 'unset', for a default OpenShift Enterprise installation it's 'asimplething')" }),
    # parameters used for setting up sshd
    [string] $sshdCygwinDir = $(if (-not $upgrade) {  'c:\openshift\cygwin' }),
    [string] $sshdListenAddress = '0.0.0.0',
    [int] $sshdPort = 22,
    # parameters used for proxy settings
    [string] $proxy = $null,
    # parameters used for skipping some installation steps
    [Switch] $skipRuby = $false,
    [Switch] $skipCygwin = $false,
    [Switch] $skipMCollective = $false,
    [Switch] $skipChecks = $false,
    [Switch] $skipGlobalEnv = $false,
    [Switch] $skipServicesSetup = $false,
    [Switch] $skipBinDirCleanup = $false,
    # parameters used when behind the firewall
    [string] $cygwinInstallerExe = [string]::Empty,
    [string] $rubyInstallerExe = [string]::Empty,
    [string] $mcollectiveGemsDir = [string]::Empty,
    # parameters used for skipping SQL Server support
    [switch] $noSql2008 = $false,
    [switch] $noSql2012 = $false
)

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
$upgradeDeployment = $false

Write-Verbose 'Loading modules and scripts ...'
Import-Module (Join-Path $currentDir '..\..\common\openshift-common.psd1') -DisableNameChecking
. (Join-Path $currentDir 'validation-helpers.ps1')
. (Join-Path $currentDir 'setup-helpers.ps1')
. (Join-Path $currentDir 'ruby-helpers.ps1')
. (Join-Path $currentDir 'service-helpers.ps1')

$global:endWarnings = @()

$upgradeDeployment = Check-OpenShiftServices

if ($upgradeDeployment)
{
    Stop-Gears
    if ($upgrade)
    {
        Write-Host "Loading existing configuration file"
        $config = Load-Config($mcollectivePath)
        if (!$publicHostname) { $publicHostname = $config["PUBLIC_HOSTNAME"] };
        if (!$publicIp) {$publicIp = $config["PUBLIC_IP"]};
        if (!$brokerHost) 
        { 
            $brokerHost = $config["BROKER_HOST"] 
            $mcollectiveActivemqServer = $config["BROKER_HOST"]
        };

        if (!$sshdCygwinDir) {  $sshdCygwinDir = Split-Path($config["SSHD_BASE_DIR"])};
        if (!$cloudDomain) { $cloudDomain = $config["CLOUD_DOMAIN"]};
        if (!$externalEthDevice){$externalEthDevice= $config["EXTERNAL_ETH_DEV"] };
        if (!$internalEthDevice){$internalEthDevice= $config["INTERNAL_ETH_DEV"] };
        if (!$gearBaseDir){$gearBaseDir= $config["GEAR_BASE_DIR"] };
        if (!$gearShell){$gearShell= $config["GEAR_SHELL"] };
        if (!$gearGecos){$gearGecos= $config["GEAR_GECOS"] };
        if (!$cartridgeBasePath){$cartridgeBasePath= $config["CARTRIDGE_BASE_PATH"] };
        if (!$platformLogFile){$platformLogFile= $config["PLATFORM_LOG_FILE"] };
        if (!$platformLogLevel){$platformLogLevel= $config["PLATFORM_LOG_LEVEL"] };
        if (!$containerizationPlugin){$containerizationPlugin= $config["CONTAINERIZATION_PLUGIN"] };
        if (!$binLocation){$binLocation= $config["BIN_DIR"] };
        if (!$mcollectivePath){$mcollectivePath= $config["MCOLLECTIVE_LOCATION"] };
        if (!$rubyInstallLocation){$rubyInstallLocation= $config["RUBY_LOCATION"] };
        if (!$mcollectivePskPlugin){$mcollectivePskPlugin= $config["plugin.psk"]};
        if (!$mcollectiveActivemqPort){$mcollectiveActivemqPort= $config["plugin.activemq.pool.1.port"]};
        if (!$mcollectiveActivemqUser){$mcollectiveActivemqUser= $config["plugin.activemq.pool.1.user"]};
        if (!$mcollectiveActivemqPassword){$mcollectiveActivemqPassword= $config["plugin.activemq.pool.1.password"]};
    }
	Start-Sleep -s 10
}


# Check to see if any processes are running
Check-RunningProcesses


Write-Host 'Installation logs will be written in c:\openshift\setup_logs'
New-Item -path 'C:\openshift\setup_logs' -type directory -Force | out-Null

# TODO: vladi: Using a hardcoded mcollective path - this is no longer necessary, we can setup mcollective using a dynamic path
$mcollectivePath = 'c:\openshift\mcollective\'


# Be verbose and print all settings
Write-Verbose "Target binary location used is '$binLocation'"
Write-Verbose "Public hostname used is '$publicHostname'"
Write-Verbose "Broker host used is '$brokerHost'"
Write-Verbose "Cloud domain used is '$cloudDomain'"
Write-Verbose "External ethernet device used is '$externalEthDevice'"
Write-Verbose "Internal ethernet device used is '$internalEthDevice'"
Write-Verbose "Public IP used is '$publicIp'"
Write-Verbose "Target gear directory used is '$gearBaseDir'"
Write-Verbose "Ger shell used is '$gearShell'"
Write-Verbose "Gecos information used is '$gearGecos'"
Write-Verbose "Target cartridge path used is '$cartridgeBasePath'"
Write-Verbose "Target platform log file used is '$platformLogFile'"
Write-Verbose "Target log level used is '$platformLogLevel'"
Write-Verbose "Container used is '$containerizationPlugin'"
Write-Verbose "Ruby download location used is '$rubyDownloadLocation'"
Write-Verbose "Target ruby installation directory used is '$rubyInstallLocation'"
Write-Verbose "ActiveMQ server used is '$mcollectiveActivemqServer'"
Write-Verbose "ActiveMQ port used is '$mcollectiveActivemqPort'"
Write-Verbose "ActiveMQ user used is '$mcollectiveActivemqUser'"
Write-Verbose "MCollective PSK plugin is '$mcollectivePskPlugin'"
Write-Verbose "Target cygwin installation dir used is '$sshdCygwinDir'"
Write-Verbose "SSHD listen address used is '$sshdListenAddress'"
Write-Verbose "SSHD listening port used is '$sshdPort'"

if ($rubyInstallerExe -ne $false)
{
    Write-Warning "Using ruby installer '$rubyInstallerExe'"
}
else
{
    Write-Warning "Ruby installer will be downloaded from '$rubyDownloadLocation'"
}

if ($cygwinInstallerExe -ne $false)
{
    Write-Warning "Using cygwin installer '$cygwinInstallerExe'"
}
else
{
    Write-Warning "Ruby installer will be downloaded from 'http://cygwin.com/setup-x86.exe'"
}

if ($mcollectiveGemsDir -ne $false)
{
    Write-Warning "Installing MCollective gems from '$mcollectiveGemsDir'"
}
else
{
    Write-Warning "MCollective gems will be downloaded from 'http://rubygems.org'"
}


if ([string]::IsNullOrWhiteSpace($proxy))
{
    Write-Verbose "Not using a proxy server."
}
else
{
    Write-Warning "Using proxy ${proxy}"
    $env:osiProxy = $proxy
    if ($proxy.StartsWith('https'))
    {
        $env:https_proxy = $proxy
    }
    else
    {
        $env:http_proxy = $proxy
    }
}


Write-Verbose "Verifying required variables are not empty ..."
if ([string]::IsNullOrWhitespace($publicHostname)) { Write-Error "Public hostname cannot be empty."; exit 1; }
if ([string]::IsNullOrWhitespace($brokerHost)) { Write-Error "Broker host cannot be empty."; exit 1; }
if ([string]::IsNullOrWhitespace($cloudDomain)) { Write-Error "Cloud domain cannot be empty."; exit 1; }

if ($skipChecks -eq $false)
{
    Write-Host 'Verifying prerequisites ...'
    Check-Elevation
    Check-WindowsVersion
    Check-VCRedistributable
    Check-Java
    $windowsFeatures = @('NET-Framework-Features', 'NET-Framework-Core', 'NET-Framework-45-Features', 'NET-Framework-45-Core', 'NET-Framework-45-ASPNET', 'NET-WCF-Services45', 'NET-WCF-TCP-PortSharing45') 
    $windowsFeatures | ForEach-Object { Check-WindowsFeature $_ }
    $iisFeatures = @('Web-Server', 'Web-WebServer', 'Web-Common-Http', 'Web-Default-Doc', 'Web-Dir-Browsing', 'Web-Http-Errors', 'Web-Static-Content', 'Web-Http-Redirect', 'Web-DAV-Publishing', 'Web-Health', 'Web-Http-Logging', 'Web-Custom-Logging', 'Web-Log-Libraries', 'Web-ODBC-Logging', 'Web-Request-Monitor', 'Web-Http-Tracing', 'Web-Performance', 'Web-Stat-Compression', 'Web-Dyn-Compression', 'Web-Security', 'Web-Filtering', 'Web-Basic-Auth', 'Web-CertProvider', 'Web-Client-Auth', 'Web-Digest-Auth', 'Web-Cert-Auth', 'Web-IP-Security', 'Web-Url-Auth', 'Web-Windows-Auth', 'Web-App-Dev', 'Web-Net-Ext', 'Web-Net-Ext45', 'Web-AppInit', 'Web-Asp-Net', 'Web-Asp-Net45', 'Web-CGI', 'Web-ISAPI-Ext', 'Web-ISAPI-Filter', 'Web-Includes', 'Web-WebSockets', 'Web-Mgmt-Tools', 'Web-Scripting-Tools', 'Web-Mgmt-Service', 'Web-WHC')
    $iisFeatures | ForEach-Object { Check-WindowsFeature $_ }
    
    if ($noSql2008 -eq $false)
    {
        Check-SQLServer2008
    }

    if ($noSql2012 -eq $false)
    {
        Check-SQLServer2012
    }

    Check-Builders
}

Write-Host 'Generating node.conf file ...'
Write-Verbose 'Creating directory c:\openshift ...'
New-Item -path 'C:\openshift\' -type directory -Force | Out-Null
Write-Template (Join-Path $currentDir "node.conf.template") "c:\openshift\node.conf" @{
    publicHostname = $publicHostname
    publicIp = $publicIp
    brokerHost = $brokerHost
    sshBaseDir = (Join-Path $sshdCygwinDir "installation")
    cloudDomain = $cloudDomain
    externalEthDev = $externalEthDevice
    internalEthDev = $internalEthDevice
    gearBaseDir = $gearBaseDir
    gearShell = $gearShell
    gearGecos = $gearGecos
    cartridgeBasePath = $cartridgeBasePath
    platformLogFile = $platformLogFile
    platformLogLevel = $platformLogLevel
    containerizationPlugin = $containerizationPlugin
    binDir = $binLocation
    mcollectiveLocation = $mcollectivePath
    rubyLocation = $rubyInstallLocation
}

Write-Host "Creating gears dir '${gearBaseDir}' ..."
New-Item -path $gearBaseDir -type directory -Force | out-Null

Write-Host 'Generating resource_limits.conf file ...'
Write-Template (Join-Path $currentDir "resource_limits.conf.template") "c:\openshift\resource_limits.conf" @{
}


if ($skipServicesSetup -eq $false)
{
    Create-OpenshiftGroup
    $openshiftServiceUserPassword = [string]::Empty
    Create-OpenshiftUser ([REF]$openshiftServiceUserPassword)
    Setup-Privileges
}

# setup MSSQL authentication
if ($noSql2008 -eq $false)
{
    Setup-Mssql2008Authentication
}

Start-Sleep -s 10

if ($noSql2010 -eq $false)
{
    Setup-Mssql2012Authentication
}

# copy binaries
Write-Host 'Copying binaries ...'
if ($skipBinDirCleanup -eq $false)
{
    Cleanup-Directory $binLocation
}
Write-Verbose "Creating bin directory '${binLocation}' ..."
New-Item -path $binLocation -type directory -Force | Out-Null
$sourceItems = (Join-Path $currentDir '..\..\..\*')
Copy-Item -Recurse -Force -Verbose:($PSBoundParameters['Verbose'] -eq $true) -Exclude 'cartridges' -Path $sourceItems $binLocation

Write-Host 'Generating native images...'
Setup-GAC($binLocation)

# setup ruby
if ($skipRuby -eq $false)
{
    Setup-Ruby $rubyDownloadLocation $rubyInstallLocation $rubyInstallerExe
}

Write-Host 'Setting up SSHD ...'
if ($skipCygwin -eq $false)
{
    Setup-SSHD $sshdCygwinDir $sshdListenAddress $sshdPort $cygwinInstallerExe
}
$cygpath = (Join-Path $sshdCygwinDir 'installation\bin\cygpath.exe')
$chmod = (Join-Path $sshdCygwinDir 'installation\bin\chmod.exe')


Write-Host 'Setting up MCollective ...'
if ($skipMCollective -eq $false)
{
    Setup-MCollective 'c:\openshift\mcollective' (Join-Path $sshdCygwinDir 'installation') $rubyInstallLocation $mcollectiveGemsDir
}
Configure-MCollective $mcollectiveActivemqServer $mcollectiveActivemqPort $mcollectiveActivemqUser $mcollectiveActivemqPassword 'c:\openshift\mcollective' $binLocation $rubyInstallLocation $mcollectivePskPlugin

# setup cartridges
Write-Host 'Copying cartridges ...'
Cleanup-Directory $cartridgeBasePath
Write-Verbose "Creating cartridges directory '${cartridgeBasePath}' ..."
New-Item -path $cartridgeBasePath -type directory -Force | Out-Null
$sourceItems = (Join-Path $currentDir '..\..\..\cartridges\*')
Copy-Item -Recurse -Force -Verbose:($PSBoundParameters['Verbose'] -eq $true) -Path $sourceItems $cartridgeBasePath

if ($skipGlobalEnv -eq $false)
{
    # setup oo-bin alias paths
    Setup-OOAliases $binLocation $sshdCygwinDir

    # setup env vars in c:\openshift\env
    Setup-GlobalEnv $binLocation
}

if ($skipServicesSetup -eq $false)
{
    Write-Host 'Setting up facts updater scheduled task ...'
    Setup-FacterScheduledTask $openshiftServiceUserPassword $binLocation

    Write-Host 'Setting up startup script to start gears ...'
    Setup-StartupScheduledTask $openshiftServiceUserPassword $binLocation

    Write-Host 'Setting up shutdown script to stop gears ...'
    Setup-StopScheduledTask $openshiftServiceUserPassword $binLocation

    Remove-Service 'openshift.mcollectived' $sshdCygwinDir
    Remove-Service 'openshift.sshd' $sshdCygwinDir
    

    $mcollectiveLib = (Join-Path $mcollectivePath 'lib').Replace("\", "/")
    $mcollectiveBin = (Join-Path $mcollectivePath 'bin\mcollectived')
    $mcollectiveConfig = (Join-Path $mcollectivePath 'etc\server.cfg')

    Create-Service $openshiftServiceUserPassword 'openshift.mcollectived' (Join-Path $rubyInstallLocation 'bin\ruby.exe') "-I'${mcollectiveLib};' -- '${mcollectiveBin}' --config '${mcollectiveConfig}'" "OpenShift Windows Node MCollective Service" $sshdCygwinDir

    $runSSHDScript = (Join-Path $binLocation 'powershell\tools\sshd\run-sshd.ps1')
    $cygwinInstallationPath = (Join-Path $sshdCygwinDir 'installation')

    Create-Service $openshiftServiceUserPassword 'openshift.sshd' (Get-Command powershell).Path "-File '${runSSHDScript}' -targetDirectory '${cygwinInstallationPath}'" "OpenShift Windows Node SSHD Service" $sshdCygwinDir "/var/run/sshd.pid"

    Write-Host 'Starting services ...'
    net start openshift.mcollectived
    net start openshift.sshd
}

if ($upgradeDeployment)
{
    Start-Gears
}

$global:endWarnings | ForEach-Object { Write-Warning $_ }

Write-Warning "Please make sure that the Linux host '${brokerHost}' can properly resolve '${publicHostname}'."
Write-Warning "Please make sure that all hosts, Windows and Linux have their clocks synchronized."


Write-Host "Done." -ForegroundColor Green