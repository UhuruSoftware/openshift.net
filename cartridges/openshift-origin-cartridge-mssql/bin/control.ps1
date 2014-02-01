param(
[string]$command
)

. $env:OPENSHIFT_CARTRIDGE_SDK_POWERSHELL

$MSSQL_PID_FILE = $env:OPENSHIFT_MSSQL_DIR+"\run\mssql.pid"
$env:MSSQL_PID_FILE = $MSSQL_PID_FILE

#Start the software the cartridge controls
function start-cartridge
{
    if (process_running "powershell" $MSSQL_PID_FILE)
    {
        Write-Host "Cartridge already running"
    }
    else
    {
        Write-Host "Starting MSSQL Cartridge"
        "Starting" > $MSSQL_PID_FILE

        $logDir = (Join-Path $env:OPENSHIFT_MSSQL_DIR 'log')
        New-Item -path $logDir -type directory -Force | Out-Null

        $job = Start-Process powershell -argument "$env:OPENSHIFT_MSSQL_DIR\bin\start.bat  1>> ${logDir}\stdout.log 2>> ${logDir}\stderr.log" -passthru -windowstyle hidden
        $job.Id > $MSSQL_PID_FILE

        $currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
        $instanceName = "Instance${env:OPENSHIFT_MSSQL_DB_PORT}"
        $instanceDir = (join-path $currentDir "MSSQL10_50.${InstanceName}")
        $passwordFile = (Join-Path $instanceDir 'sqlpasswd')
        $password = [IO.File]::ReadAllText( $passwordFile )

        client_result ""
        client_result "Microsoft SQL Server 2008 database added.  Please make note of these credentials:"
        client_result ""
        client_result "     sa password: ${password}"
        client_result "   database name: ${env:OPENSHIFT_APP_NAME}"
        client_result ""

        client_result "Connection URL: mssql://`$OPENSHIFT_MSSQL_DB_HOST:`$OPENSHIFT_MSSQL_DB_PORT/"
        client_result ""
    }

    exit 0
}

#Stop the software the cartridge controls
function stop-cartridge
{
    if (process_running "powershell" $MSSQL_PID_FILE)
    {
        Write-Host "Stopping"
        $jobid = [int](Get-Content $MSSQL_PID_FILE)
        Remove-Item $MSSQL_PID_FILE
        taskkill /F /T /PID $jobid
        Stop-Process -Id $jobid -Force -ErrorAction SilentlyContinue
    }
    else
    {
        Write-Output "Cartridge is not running"
    }
}

#Return an 0 exit status if the cartridge code is running
function status-cartridge
{
    Write-Host "Retrieving cartridge"
    if (process_running "powershell" $MSSQL_PID_FILE)
    {
        client_result "Application is running"
    }
    else
    {
        client_result "Application is either stopped or inaccessible"
    }
}

#The cartridge and the packaged software needs to re-read their configuration information 
#(this operation will only be called if your cartridge is running)
function reload-cartridge
{
    Write-Output "Reloading cartridge"
    restart-cartridge
}

#Stop current process and start a new one for the code the cartridge packages
function restart-cartridge
{
    "Restarting cartridge"
    stop-cartridge
    start-cartridge
}

#If applicable, your cartridge should signal the packaged software to perform a thread dump
function threaddump-cartridge
{
}

#All unused resources should be released 
#(it is at your discretion to determine what should be done; be frugal as on some systems resources may be very limited)
function tidy-cartridge
{

}

#Prepare the cartridge for a snapshot, e.g. dump database to flat file
function pre-snapshot-cartridge
{

}

#Clean up the cartridge after snapshot, e.g. remove database dump file
function post-snapshot-cartridge
{

}

#Prepare the cartridge for restore
function pre-restore-cartridge
{

}

#Clean up the cartridge after being restored, load database with data from flat file
function post-restore-cartridge
{

}

switch ($command)
  {
    "start" { start-cartridge }
    "stop" { stop-cartridge }
    "status" { status-cartridge }
    "reload" { reload-cartridge }
    "restart" { restart-cartridge }
    "threaddump" { threaddump-cartridge }
    "tidy" { tidy-cartridge }
    "pre-snapshot" { pre-snapshot-cartridge }
    "post-snapshot" { post-snapshot-cartridge }
    "pre-restore" { pre-restore-cartridge }
    "post-restore" { post-restore-cartridge }
  }