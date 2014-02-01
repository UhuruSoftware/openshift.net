<#
.SYNOPSIS
    OpenShift Windows Node packaging and installation bootstrap script.
.DESCRIPTION
    This script packages all the OpenShift Windows Node files into an self-extracting file.
    Upon self-extraction this script is run to unpack files and start the installation process.

.PARAMETER action
    This is the parameter that specifies what the script should do: package the node or bootstrap the installation process.

.NOTES
    Author: Vlad Iovanov
    Date:   January 27, 2014

.EXAMPLE
.\install.ps1 -publicHostname winnode-001.mycloud.com -brokerHost broker.mycloud.com -cloudDomain mycloud.com
Install the node by passing the minimum information required. 
.EXAMPLE
.\install.ps1 -publicHostname winnode-001.mycloud.com -brokerHost broker.mycloud.com -cloudDomain mycloud.com -publicIP 10.2.0.104
Install the node by also passing the public IP address of the machine.
#>
param (
    [Parameter(Mandatory=$true)]
    [ValidateSet('package','bootstrap')]
    [string] $action
)

function DoAction-Package()
{
    Write-Host 'Packaging windows node files from the .\output dir ...'
    [Reflection.Assembly]::LoadWithPartialName( "System.IO.Compression.FileSystem" ) | out-null

    $src_folder = ".\output"
    $destfile = ".\output.zip"
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    $includebasedir = $false
    Remove-Item -Force -Path $destfile -ErrorAction SilentlyContinue

    Write-Host 'Creating zip ...'

    [System.IO.Compression.ZipFile]::CreateFromDirectory($src_folder,$destfile,$compressionLevel, $includebasedir )

    Write-Host 'Creating the self extracting exe ...'

    $installerProcess = Start-Process -Wait -PassThru -NoNewWindow 'iexpress' "/N /Q winnode-installer.sed"

    if ($installerProcess.ExitCode -ne 0)
    {
        Write-Error "There was an error building the installer."
        exit 1
    }
    
    Write-Host 'Removing artifacts ...'
    Remove-Item -Force -Path $destfile -ErrorAction SilentlyContinue
    
    Write-Host 'Done.'
}

function DoAction-Bootstrap()
{
    Write-Host 'Installing the Uhuru OpenShift Windows Node ...'

    [Reflection.Assembly]::LoadWithPartialName( "System.IO.Compression.FileSystem" ) | out-null
    $src_file = ".\output.zip"
    $destfolder = ".\output"

    Write-Host 'Unpacking files ...'
    [System.IO.Compression.ZipFile]::ExtractToDirectory($src_file,$destfolder)
    
    Write-Host 'Starting installation ...'
    cd .\output\powershell\tools\openshift.net\
    get-help -full .\install.ps1
}

if ($action -eq 'package')
{
    DoAction-Package
}
elseif ($action -eq 'bootstrap')
{
    DoAction-Bootstrap
}

