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

function Check-SQLServer2008()
{
    Write-Verbose "Checking if MS SQL Server 2008 is installed ..."

    $mssqlRegistry = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL10_50.MSSQLSERVER\Setup')

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