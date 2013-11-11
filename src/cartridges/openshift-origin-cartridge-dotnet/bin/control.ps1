param(
[string]$command
)

#for testing
$env:OPENSHIFT_DOTNET_DIR = "D:\_code\openshift.net\src\cartridges\openshift-origin-cartridge-dotnet\"
$env:OPENSHIFT_DOTNET_IP = 8080
$env:OPENSHIFT_REPO_DIR ="D:\_code\openshift.net\src\cartridges\openshift-origin-cartridge-dotnet\usr\template"



$HTTPD_PID_FILE = $env:OPENSHIFT_DOTNET_DIR+"\run\httpd.pid"
$env:HTTPD_PID_FILE = $HTTPD_PID_FILE

#Start the software the cartridge controls
function start-cartridge
{
  Write-Host "Starting"
  "Starting" > $HTTPD_PID_FILE
  $job = Start-Job -filepath  $env:OPENSHIFT_REPO_DIR"\dotnet\webserver.ps1"
  $job.Id > $HTTPD_PID_FILE
}

#Stop the software the cartridge controls
function stop-cartridge
{
  Write-Host "Stopping"
  $jobid = [int](Get-Content $HTTPD_PID_FILE)
  Remove-Item $HTTPD_PID_FILE
  Invoke-WebRequest -Uri "http://localhost:$env:OPENSHIFT_DOTNET_IP"
  Receive-Job $jobid
  Stop-Job $jobid
  Remove-Job $jobid
  #need one more get request
  
}

#Return an 0 exit status if the cartridge code is running
function status-cartridge
{
  Write-Host "Retrieving cartridge"
  if (Test-Path $HTTPD_PID_FILE)
  {
    $jobid = [int](Get-Content $HTTPD_PID_FILE)
    $job = Get-Job $jobid
    if ($job.State -eq "Running" )
    {
        Write-Host "Application is running"
    }
    else
    {
        Write-Host "Application is either stopped or inaccessible"
    }
  }
  else
  {
    Write-Host "Application is either stopped or inaccessible"
  }
  
}

#The cartridge and the packaged software needs to re-read their configuration information 
#(this operation will only be called if your cartridge is running)
function reload-cartridge
{
  "Reloading cartridge"
  restart-cartridge
}

#Stop current process and start a new one for the code the cartridge packages
function restart-cartridge
{
  "Restarting cartdrige"
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