param(
[string]$command
)

#for testing

. $env:OPENSHIFT_CARTRIDGE_SDK_POWERSHELL

$HTTPD_PID_FILE = $env:OPENSHIFT_WINSAMPLE_DIR+"\run\httpd.pid"
$env:HTTPD_PID_FILE = $HTTPD_PID_FILE

#Start the software the cartridge controls
function start-cartridge
{
    if (process_running "powershell" $HTTPD_PID_FILE)
    {
        Write-Host "Cartridge already running"
    }
    else
    {
		Write-Host "Starting"
		"Starting" > $HTTPD_PID_FILE

		$logDir = (Join-Path $env:OPENSHIFT_WINSAMPLE_DIR 'log')
		New-Item -path $logDir -type directory -Force | Out-Null

		$job = Start-Process powershell -argument "$env:OPENSHIFT_REPO_DIR\winsample\webserver.ps1 1>> ${logDir}\stdout.log 2>> ${logDir}\stderr.log" -passthru -windowstyle hidden
		$job.Id > $HTTPD_PID_FILE
	}
}

#Stop the software the cartridge controls
function stop-cartridge
{
    if (process_running "powershell" $HTTPD_PID_FILE)
    {
        Write-Host "Stopping"
		$jobid = [int](Get-Content $HTTPD_PID_FILE)
		Remove-Item $HTTPD_PID_FILE
		Invoke-WebRequest -Uri "http://${env:OPENSHIFT_APP_DNS}:${env:OPENSHIFT_WINSAMPLE_PORT}"
		Stop-Process $jobid
		#need one more get request  
    }
	else
	{
		write-output "Cartridge not running"
	}
}

#Return an 0 exit status if the cartridge code is running
function status-cartridge
{
	Write-Host "Retrieving cartridge"
	if (process_running "powershell" $HTTPD_PID_FILE)
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