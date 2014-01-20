function Cleanup-Directory($directory)
{
    if (Test-Path -Path $directory)
    {
        Write-Verbose "Directory '${directory}' exists, cleaning it up."
        Remove-Item -Path $directory -Force -Recurse
    }
}

function Setup-MCollective()
{
    $mcollectiveSetupScript = (Join-Path $currentDir '..\mcollective\setup-mcollective.ps1')
    $mcollectiveSetupProcess = Start-Process -Wait -PassThru -NoNewWindow 'powershell' "-File ${mcollectiveSetupScript}"

    if ($mcollectiveSetupProcess.ExitCode -ne 0)
    {
        Write-Error 'MCollective setup failed. Please check installation logs.'
        exit 1
    }
    else
    {
        Write-Host "[OK] MCollective installed successfuly."
    }
}

function Configure-MCollective($userActivemqServer, $userActivemqPort, $userActivemqUser, $userActivemqPassword, $binDir)
{
    $mcollectiveSetupScript = (Join-Path $currentDir '..\mcollective\configure-mcollective.ps1')

    $arguments = "-File ${mcollectiveSetupScript} -userActivemqServer ${userActivemqServer} -userActivemqPort ${userActivemqPort} -userActivemqUser ${userActivemqUser} -userActivemqPassword ${userActivemqPassword} -binDir ${binDir}"
    $mcollectiveSetupProcess = Start-Process -Wait -PassThru -NoNewWindow 'powershell' $arguments
     
    if ($mcollectiveSetupProcess.ExitCode -ne 0)
    {
        Write-Error 'MCollective configuration failed. Please check installation logs.'
        exit 1
    }
    else
    {
        Write-Host "[OK] MCollective configured successfuly."
    }
}

function Setup-SSHD($cygwinDir, $listenAddress, $port)
{
    $sshdSetupScript = (Join-Path $currentDir '..\sshd\setup-sshd.ps1')

    $arguments = "-File ${sshdSetupScript} -cygwinDir ${cygwinDir} -listenAddress ${listenAddress} -port ${port}"
    $sshdSetupProcess = Start-Process -Wait -PassThru -NoNewWindow 'powershell' $arguments

    if ($sshdSetupProcess.ExitCode -ne 0)
    {
        Write-Error 'SSHD setup failed. Please check installation logs.'
        exit 1
    }
    else
    {
        Write-Host "[OK] SSHD installed successfuly."
    }
}

function Setup-OOAliases($binLocation)
{
    $ooBinDir = "c:\openshift\oo-bin"
    Cleanup-Directory $ooBinDir
    Write-Host "Setting bash aliases for oo-* powershell commands in '${ooBinDir}' ..."
    Write-Verbose "Creating oo-bin directory '${ooBinDir}' ..."
    New-Item -path $ooBinDir -type directory -Force | Out-Null
    $ooPowerShelDir = (Join-Path $binLocation 'powershell\OO-Cmdlets\')
    $ooScripts = Get-ChildItem -Path $ooPowerShelDir -Filter "*.ps1" 
    foreach ($ooScript in $ooScripts)
    {
        $scriptUnixPath = & $cygpath $ooScript.FullName
        $aliasPath = (Join-Path $ooBinDir $ooScript.Name.SubString(0, $ooScript.Name.Length - 4).ToLower())
        "powershell -File ${scriptUnixPath} `$@" | Out-File -Encoding Ascii -Force -FilePath $aliasPath
        $aliasUnixPath = & $cygpath $aliasPath
        & $chmod +x $aliasUnixPath
    }
}

function Setup-GlobalEnv
{
    $ooBinDir = "c:\openshift\oo-bin"
    $envDir = "c:\openshift\env"
    Cleanup-Directory $envDir
    Write-Host "Setting up global gear environment variables in '${envDir}' ..."
    Write-Verbose "Creating env directory '${envDir}' ..."
    New-Item -path $envDir -type directory -Force | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $envDir 'OPENSHIFT_BROKER_HOST'), $brokerHost)
    [System.IO.File]::WriteAllText((Join-Path $envDir 'OPENSHIFT_CLOUD_DOMAIN'), $cloudDomain)

    $pathEnvEntries =@('/usr/local/bin',
        '/usr/bin',
        (& $cygpath ([environment]::getfolderpath("system"))),
        (& $cygpath ([environment]::getfolderpath("windows"))),
        (& $cygpath (join-Path ([environment]::getfolderpath("system")) 'wbem')),
        (& $cygpath (join-Path ([environment]::getfolderpath("system")) 'windowspowershell\v1.0')),
        (& $cygpath $ooBinDir),
        (& $cygpath (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\100\Tools\ClientSetup').Path))

    [System.IO.File]::WriteAllText((Join-Path $envDir 'PATH'), [string]::Join(":", $pathEnvEntries))
}

function Setup-Ruby($rubyDownloadLocation, $rubyInstallLocation)
{
    Write-Host "Downloading ruby setup package from '${rubyDownloadLocation}'"
    $rubySetupPackage = Join-Path $env:TEMP "ruby-setup.exe"
    if ((Test-Path $rubySetupPackage) -eq $true)
    {
        Write-Verbose "Removing existing ruby setup package from temp dir."
        rm $rubySetupPackage -Force > $null
    }
    Invoke-WebRequest $rubyDownloadLocation -OutFile $rubySetupPackage
    Write-Verbose "Ruby install package downloaded to '${rubySetupPackage}'"

    Cleanup-Directory $rubyInstallLocation

    Write-Host "Installing ruby to '${rubyInstallLocation}' ..."
    $rubySetupProcess = Start-Process -Wait -PassThru -NoNewWindow $rubySetupPackage "/verysilent /dir=""${rubyInstallLocation}"""

    if ($rubySetupProcess.ExitCode -ne 0)
    {
        Write-Error 'Ruby setup failed. Please check installation logs.'
        exit 1
    }
    else
    {
        Write-Host "[OK] Ruby installed successfuly."
    }
}

function Setup-RubyDevkit($rubyDevKitDownloadLocation, $rubyDevKitInstallLocation)
{
    Write-Host "Downloading ruby devkit package from '${rubyDevKitDownloadLocation}'"
    $rubyDevkitSetupPackage = Join-Path $env:TEMP "rubydevkit-setup.exe"
    if ((Test-Path $rubyDevkitSetupPackage) -eq $true)
    {
        Write-Verbose "Removing existing ruby devkit setup package from temp dir."
        rm $rubyDevkitSetupPackage -Force > $null
    }
    Invoke-WebRequest $rubyDevKitDownloadLocation -OutFile $rubyDevkitSetupPackage
    Write-Verbose "Ruby devkit install package downloaded to '${rubyDevkitSetupPackage}'"
    
    Cleanup-Directory $rubyDevKitInstallLocation

    Write-Host "Installing ruby devkit to '${rubyDevKitInstallLocation}' ..."
    $rubyDevkitSetupProcess = Start-Process -Wait -PassThru -NoNewWindow $rubyDevkitSetupPackage "-o""${rubyDevKitInstallLocation}"" -y"

    if ($rubyDevkitSetupProcess.ExitCode -ne 0)
    {
        Write-Error 'Ruby devkit setup failed. Please check installation logs.'
        exit 1
    }
    else
    {
        Write-Host "[OK] Ruby devkit installed successfuly."
    }
}