
function Create-OpenshiftUser()
{
    $username = 'openshift_service'

    Write-Host "Checking if the '${username}' user exists ..."
    [ADSI]$localServer="WinNT://${env:COMPUTERNAME}"
    
    if ([ADSI]::Exists("WinNT://${env:COMPUTERNAME}/${username}"))
    {
        try
        {
            Write-Host "Deleting the '${username}' user ..."
            $localServer.Delete("user", $username)
        }
        catch
        {
            Write-Error "Could not remove the '${username}' user."
            exit 1
        }
    }
    try
    {
        Write-Host "Creating local '${username}' user ..."

        Write-Host "Generating password for '${username}' user ..."
        $password = "P1_" + (Get-RandomPassword)

        $objOu = [ADSI]"WinNT://${env:COMPUTERNAME}"
        $objUser = $objOu.Create('user', $username)
        $objUser.setpassword($password)
        $objUser.SetInfo()
        $objUser.description = "Openshift Windows Node User Account"
        $objUser.SetInfo()

        Write-Host "Done creating '${username}' user. Adding it to the administrators group ..."

        $group = [ADSI]("WinNT://${env:COMPUTERNAME}/administrators,group")
        $group.add("WinNT://${env:COMPUTERNAME}/${username},user")

        return $password
    }
    catch [Exception]
    {
        $exceptionMessage = $_.Exception.Message
        Write-Error "Could not create the '${username}' user - ${exceptionMessage}"
        exit 1
    }
}

function Create-Service($serviceAccountPassword, $name, $exe, $exeArgs, $description, $cygwinPath, $pidFile = $null)
{
    Write-Verbose "Looking up binaries in '${cygwinPath}' ..."

    $serviceUsername = 'openshift_service'

    $cygrunsrv = (Join-Path $cygwinPath 'installation\bin\cygrunsrv.exe')
    $cygpath = (Join-Path $cygwinPath 'installation\bin\cygpath.exe')

    if ((Test-Path $cygpath) -ne $true)
    {
        Write-Error "Can't find 'cygpath'. Exiting."
        exit 1
    }

    if ((Test-Path $cygrunsrv) -ne $true)
    {
        Write-Error "Can't find 'cygrunsrv'. Exiting."
        exit 1
    }

    $cygExe = & $cygpath $exe
    $cygrunsrvArgs = "-I ""${name}"" -p ""${cygExe}"" -a ""${exeArgs}"" -f ""${description}"" -u ""${serviceUsername}"" -w ${serviceAccountPassword}"

    if ($pidFile -ne $null)
    {
        $cygrunsrvArgs = "${cygrunsrvArgs} -x ${pidFile}"
    }

    Write-Verbose "Using the following arguments to setup the '${name}' service: ${cygrunsrvArgs}"

    $cygrunsrvProcess = Start-Process -Wait -PassThru -NoNewWindow $cygrunsrv $cygrunsrvArgs

    if ($cygrunsrvProcess.ExitCode -ne 0)
    {
        Write-Error "Error running setting up service '${name}' with command '${command}'."
        exit 1
    }
    else
    {
        Write-Host "[OK] Service '${name}' was setup successfully."
    }
}


function Remove-Service($name, $cygwinPath)
{
     Write-Verbose "Looking up binaries in '${cygwinPath}' ..."

    $cygrunsrv = (Join-Path $cygwinPath 'installation\bin\cygrunsrv.exe')

    if ((Test-Path $cygrunsrv) -ne $true)
    {
        Write-Error "Can't find 'cygrunsrv'. Exiting."
        exit 1
    }

    $cygrunsrvArgs = "-R ""${name}"""

    $cygrunsrvProcess = Start-Process -Wait -PassThru -NoNewWindow $cygrunsrv $cygrunsrvArgs
}