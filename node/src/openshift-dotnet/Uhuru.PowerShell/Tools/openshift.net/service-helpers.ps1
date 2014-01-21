
function Create-Service($name, $exe, $exeArgs, $description, $cygwinPath, $pidFile = $null)
{
    Write-Verbose "Looking up binaries in '${cygwinPath}' ..."

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
    $cygrunsrvArgs = "-I ""${name}"" -p ""${cygExe}"" -a ""${exeArgs}"" -f ""${description}"""

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