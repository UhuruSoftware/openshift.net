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
#>
param (
    [Parameter(Mandatory=$true)]
    [ValidateSet('package','bootstrap')]
    [string] $action
)

if (($pshome -like "*syswow64*") -and ((Get-WmiObject Win32_OperatingSystem).OSArchitecture -like "64*")) {
    write-warning "Restarting script under 64 bit powershell"
 
    $powershellLocation = join-path ($pshome -replace "syswow64", "sysnative") "powershell.exe"
    
    # relaunch this script under 64 bit shell
    & $powershellLocation -file $SCRIPT:MyInvocation.MyCommand.Path -action $action
 
    # This will exit the original powershell process. This will only be done in case of an x86 process on a x64 OS.
    exit
}

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
    $destfolder = "c:\openshift\installer"
    
    Write-Host "Cleaning up directory $destfolder"
    Remove-Item -Force -Recurse -Path $destfolder -ErrorVariable errors -ErrorAction SilentlyContinue

    if ($errs.Count -eq 0)
    {
        Write-Host "Successfuly cleaned the installation directory ${destfolder}"
    }
    else
    {
        Write-Error "There was an error cleaning up the installation directory '${destfolder}'.`r`nPlease make sure the folder and any of its child items are not in use, then run the installer again."
        exit 1;
    }

    Write-Host "Setting up directory $destfolder"
    New-Item -path $destfolder -type directory -Force -ErrorAction SilentlyContinue

    New-Item -path 'C:\openshift\setup_logs' -type directory -Force | out-Null

    Write-Host 'Unpacking files ...'
    try
    {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($src_file, $destfolder)
    }
    catch
    {
        Write-Error "There was an error writing to the installation directory '${destfolder}'.`r`nPlease make sure the folder and any of its child items are not in use, then run the installer again."
        exit 1;
    }
    
    cd 'c:\openshift\installer\powershell\tools\openshift.net\'

    powershell Get-Help -Full .\install.ps1
}

if ($action -eq 'package')
{
    DoAction-Package
}
elseif ($action -eq 'bootstrap')
{
    DoAction-Bootstrap
}

