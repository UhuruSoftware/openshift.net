
function Create-OpenshiftGroup
{
    Write-Host "Creating openshift group"
    $groupname = 'openshift_admins'
    [ADSI]$localServer="WinNT://${env:COMPUTERNAME}"

    if ([ADSI]::Exists("WinNT://${env:COMPUTERNAME}/${groupname},group"))
    {
       Write-Host("User group exists, skipping")
    }
    else
    {
        try
        {
        
            Write-Host "Creating local '${groupname}' group ..."
            $objOu = [ADSI]"WinNT://${env:COMPUTERNAME}"
        
            $objUser = $objOU.Create("Group", $groupname)
            $objUser.SetInfo()
            $objUser.description = "Openshift Windows Node Admin Group"
            $objUser.SetInfo()
        }
        catch [System.Exception]
        {
            $exceptionMessage = $_.Exception.Message
            Write-Error "Could not create group '${group}' - ${exceptionMessage}"
            exit 1
        }
    }
}

function Create-OpenshiftUser([REF]$password)
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
        $passwordRandomness = Get-RandomPassword
        $password.Value = "P1_${passwordRandomness}"

        $objOu = [ADSI]"WinNT://${env:COMPUTERNAME}"
        $objUser = $objOu.Create('user', $username)
        $objUser.setpassword($password.Value)
        $objUser.SetInfo()
        $objUser.description = "Openshift Windows Node User Account"
        $objUser.SetInfo()

        Write-Host "Done creating '${username}' user. Adding it to the administrators group ..."

        $group = [ADSI]("WinNT://${env:COMPUTERNAME}/administrators,group")
        $group.add("WinNT://${env:COMPUTERNAME}/${username},user")

        Write-Host "Adding user to openshift admins ..."
        $group = [ADSI]("WinNT://${env:COMPUTERNAME}/openshift_admins,group")
        $group.add("WinNT://${env:COMPUTERNAME}/${username},user")


        Write-Host "Checking current user ..."
        $currentUser = [Environment]::UserName
        $isInGroup = $false

        $group = [ADSI]("WinNT://${env:COMPUTERNAME}/openshift_admins,group")
        $group.psbase.invoke("members") | foreach {
          $username = $_.gettype().invokemember("Name", "GetProperty", $null, $_, $null)
          if ($username -eq $currentUser) {
            $isInGroup = $true
          }

        } 

        if ($isInGroup){
            Write-Host("Current user is in openshift admin group")
        }
        else{
            Write-Host("Adding current user to openshift admins")
           # $group = [ADSI]("WinNT://${env:COMPUTERNAME}/openshift_admins,group")
           # $group.add("WinNT://${env:COMPUTERNAME}/${currentUser},user")
        }

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
    $cygrunsrvArgs = "-I ""${name}"" -p ""${cygExe}"" -a ""${exeArgs}"" -f ""${description}"" -u ""${serviceUsername}"" -w ${serviceAccountPassword} -y LanmanServer"

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

function Setup-FacterScheduledTask($serviceAccountPassword, $binDir)
{
    $taskName = 'openshift.facts'
    $serviceUsername = 'openshift_service'

    $existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue

    if ($existingTask -ne $null)
    {
        Write-Host "Removing scheduled task '${taskName}' ..."

        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    }

    $interval = New-TimeSpan -Minutes 1
    $duration = [timespan]::MaxValue
    $triggerTime = [DateTime]::Now.AddMinutes(1)

    $recurringTime = New-ScheduledTaskTrigger -Once -At $triggerTime -RepetitionInterval $interval -RepetitionDuration $duration
    $powershellExe = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    $factScript = (Join-Path $binDir "powershell\tools\mcollective\update-facts.ps1")
    $action = New-ScheduledTaskAction -Execute $powershellExe -Argument "-ExecutionPolicy Bypass -File ""${factScript}"""
    
    Register-ScheduledTask -TaskName $taskName -Trigger $recurringTime -User "${env:env:COMPUTERNAME}\${serviceUsername}" –Action $action -Password $serviceAccountPassword.ToString()
}

function Setup-StartupScheduledTask($serviceAccountPassword, $binDir)
{
    $taskName = 'openshift.startup'
    $serviceUsername = 'openshift_service'

    $existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue

    if ($existingTask -ne $null)
    {
        Write-Host "Removing scheduled task '${taskName}' ..."

        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    }

    $recurringTime = New-ScheduledTaskTrigger -AtStartup
    $powershellExe = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    $factScript = (Join-Path $binDir "powershell\OO-Cmdlets\oo-admin-ctl-gears.ps1")
    $action = New-ScheduledTaskAction -Execute $powershellExe -Argument "-ExecutionPolicy Bypass -File ""${factScript}"" startall"
    
    Register-ScheduledTask -TaskName $taskName -Trigger $recurringTime -User "${env:env:COMPUTERNAME}\${serviceUsername}" –Action $action -Password $serviceAccountPassword.ToString()
}

function Setup-StopScheduledTask($serviceAccountPassword, $binDir)
{
    $taskName = 'openshift.shutdown'
    $serviceUsername = 'openshift_service'

    $existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue

    if ($existingTask -ne $null)
    {
        Write-Host "Removing scheduled task '${taskName}' ..."

        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    }

    $powershellExe = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    $factScript = (Join-Path $binDir "powershell\OO-Cmdlets\oo-admin-ctl-gears.ps1")
    $action = New-ScheduledTaskAction -Execute $powershellExe -Argument "-ExecutionPolicy Bypass -File ""${factScript}"" stopall"
    
    Register-ScheduledTask -TaskName $taskName -User "${env:env:COMPUTERNAME}\${serviceUsername}" –Action $action -Password $serviceAccountPassword.ToString()
}

function Stop-Gears
{
    Write-Host "Stopping Gears"
    Start-ScheduledTask -TaskName openshift.shutdown
}

function Start-Gears
{
    Write-Host "Starting Gears"
    Start-ScheduledTask -TaskName openshift.startup
}
