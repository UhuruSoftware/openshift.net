$global:cachedWindowsFeatures = $null

function Check-Elevation()
{
    Write-Verbose "Checking if the script is running as an administrator ..."

    If (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
    {
        Write-Error "This script requires elevation. Please run as administator."
        exit 1
    }
    else
    {
        Write-Host "[OK] Script is running as an administrator."
    }
}

function Check-WindowsVersion()
{
    Write-Verbose "Checking if the Windows Version is correct ..."

    if (([Environment]::OSVersion.Version.Major -ne 6) -or (([Environment]::OSVersion.Version.Minor -ne 2) -and ([Environment]::OSVersion.Version.Minor -ne 3)))
    {
        Write-Error "Operating system not supported. Supported Windows versions are Windows Server 2012, Windows Server 2012 R2"
        exit 1
    }
    else
    {
        Write-Host "[OK] Windows Version is correct."
    }
}

function Get-CachedWindowsFeatures()
{
    if ($global:cachedWindowsFeatures -eq $null) 
    { 
        $global:cachedWindowsFeatures = Get-WindowsFeature 
    }

    return $global:cachedWindowsFeatures
}

function Check-WindowsFeature($featureName)
{
    $feature = (Get-CachedWindowsFeatures | Where {$_.Name -eq $featureName})
    $featureDescription = $feature.DisplayName
    
    
    Write-Verbose "Checking if ${featureDescription} is installed ..."

    if ($feature.Installed -ne $true)
    {
        Write-Error "Prerequisite ${featureDescription} is not installed. Please install this and then run this script again."
        exit 1
    }
    else
    {
        Write-Host "[OK] Prerequisite ${featureDescription} is installed."
    }
}

function Check-OpenShiftServices()
{
    Write-Verbose "Checking if local OpenShift services are stopped ..."

    $sshdService = (get-service 'openshift.sshd' -ErrorAction SilentlyContinue)

    if (($sshdService -ne $null) -and ($sshdService.Status -ne "Stopped"))
    {
        Write-Error "The openshift.sshd service is running. Please stop it and then run the install script again."
        exit 1
    }

    $mcollectivedService = (get-service 'openshift.mcollectived' -ErrorAction SilentlyContinue)

    if (($mcollectivedService -ne $null) -and ($mcollectivedService.Status -ne "Stopped"))
    {
        Write-Error "The openshift.mcollectived service is running. Please stop it and then run the install script again."
        exit 1
    }
}

function Check-SQLServer2008()
{
    Write-Verbose "Checking if MS SQL Server 2008 is installed ..."

    $mssqlRegistry = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL10_50.MSSQLSERVER\Setup' -ErrorAction SilentlyContinue)

    if (($mssqlRegistry -eq $null) -or ((Test-Path $mssqlRegistry.SQLPath) -eq $false))
    {
        Write-Error "Prerequisite SQL Server 2008 is not installed. Please install this and then run this script again."
        exit 1
    }

    if ((get-service MSSQLSERVER).Status -ne "Stopped")
    {
        Write-Error "The SQL Server 2008 service 'MSSQLSERVER' is running. Please stop and disable the service and then run this script again."
        exit 1
    }

    $sqlServerStartMode = Get-WMIObject win32_service -filter "name='mssqlserver'" -computer "." | select -expand startMode

    if ($sqlServerStartMode -ne "Disabled")
    {
        Write-Error "The SQL Server 2008 service 'MSSQLSERVER' is not disabled. Please disable the service and then run this script again."
        exit 1
    }

    Write-Host "[OK] Prerequisite SQL Server 2008 is installed."
}


function Check-VCRedistributable()
{
    $vcRegistry = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\DevDiv\VC\Servicing\12.0\RuntimeMinimum' -ErrorAction SilentlyContinue)

    if (($vcRegistry -eq $null) -or ($vcRegistry.Install -ne 1))
    {
        Write-Error "Prerequisite Visual C++ Redistributable for Visual Studio 2013 is not installed. Please install this and then run this script again."
        exit 1
    }

    Write-Host "[OK] Prerequisite Visual C++ Redistributable for Visual Studio 2013 is installed."
}

function Check-Product($productName, $products)
{
    $product = $products | Where {$_.Name -eq $productName }
    if ($product -eq $null)
    {
        Write-Error "Prerequisite ${productName} is not installed. Please install this and then run this script again."
        exit 1
    }

    Write-Host "[OK] Prerequisite ${productName} is installed."
}

function Check-Builders()
{
    $products = Get-WmiObject Win32_Product

    # http://www.microsoft.com/en-us/download/details.aspx?id=40764
    Check-Product 'Microsoft Visual Studio 2013 Shell (Isolated)' $products

    # http://www.microsoft.com/en-us/download/details.aspx?id=30670
    Check-Product 'Microsoft Visual Studio 2012 Shell (Isolated)' $products

    # http://www.microsoft.com/en-us/download/details.aspx?id=1366
    Check-Product 'Microsoft Visual Studio 2010 Shell (Isolated) - ENU' $products

    # http://www.microsoft.com/en-us/download/details.aspx?id=7036
    Check-Product 'Microsoft Visual Studio Shell 2008 - ENU' $products
}