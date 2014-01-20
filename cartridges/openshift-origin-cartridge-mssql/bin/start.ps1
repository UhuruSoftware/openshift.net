#$env:OPENSHIFT_MSSQL_DB_PORT = 1433
#$env:OPENSHIFT_APP_NAME = "testapp"

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
$mssqlBase = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL10_50.MSSQLSERVER\Setup').SQLPath
$port = $env:OPENSHIFT_MSSQL_DB_PORT
$instanceName = "Instance${port}"
$instanceDir = (join-path $currentDir "MSSQL10_50.${InstanceName}")
$dbName = $env:OPENSHIFT_APP_NAME
$username = "sa"
$initialPassword = "password1234!"

function Get-Password() 
{
    $length=10
    $alphabet=$NULL
    For ($a=65; $a -le 90; $a++) 
    {
        $alphabet+=,[char][byte]$a 
    }
    
    For ($loop=1; $loop -le $length; $loop++) 
    {
        $TempPassword+=($alphabet | GET-RANDOM)
    }

    return $TempPassword
}

$sysadminPassword = "P1!" + (Get-Password)

[io.file]::WriteAllText("${env:OPENSHIFT_MSSQL_DIR}\env\OPENSHIFT_MSSQL_DB_USERNAME", $username)
[io.file]::WriteAllText("${env:OPENSHIFT_MSSQL_DIR}\env\OPENSHIFT_MSSQL_DB_PASSWORD", $sysadminPassword)

function Write-Template {
    param(
        [string]$inFile,
        [string]$outFile,
        [System.Collections.Hashtable] $variables
    )

    foreach ($key in $variables.Keys){
        New-Variable -Name $key -Value $variables[$key]
    }
    
    $fullScript = [ScriptBlock]::Create("
`$content = [IO.File]::ReadAllText( `$inFile )
Invoke-Expression `"@```"``r``n`$content``r``n```"@`" | Out-File $outFile -Encoding ascii
")

    & $fullScript
}

# Setup registry stuff
$registryFile = (Join-Path $currentDir "sqlserver.reg")
Write-Template (Join-Path $currentDir "sqlserver.reg.template") $registryFile @{
    InstanceName = $instanceName
    BaseDir = $currentDir.Replace("\", "\\")
    tcpPort = $port
}
& regedit.exe /s $registryFile

# Grab SQL Server base files and copy them to home directory
Remove-Item $instanceDir -Recurse -Force
mkdir $instanceDir -Force
Copy-Item $mssqlBase $instanceDir -recurse -force

# Start the new instance
$process = (start-process (Join-Path $instanceDir "mssql\binn\sqlservr.exe") "-c -s ${instanceName}" -Passthru)

$sqlcmd = (get-command sqlcmd).path

while ((Start-Process -FilePath $sqlcmd -ArgumentList "-Q ""EXEC sp_databases"" -U sa -P ${initialPassword} -S ""tcp:127.0.0.1,${port}""" -Wait -Passthru).ExitCode -ne 0)
{
    Start-Sleep -s 1
}

Start-Process -FilePath $sqlcmd "-Q ""EXEC sp_password NULL, '${sysadminPassword}', 'sa'"" -U sa -P ${initialPassword} -S ""tcp:127.0.0.1,${port}""" -Wait -Passthru
start-process -FilePath $sqlcmd "-Q ""CREATE DATABASE [${dbName}]"" -U sa -P ${sysadminPassword} -S ""tcp:127.0.0.1,${port}"""  -Passthru -Wait

$process.WaitForExit()