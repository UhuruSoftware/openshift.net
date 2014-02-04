#$env:OPENSHIFT_MSSQL_DB_PORT = 1433
#$env:OPENSHIFT_APP_NAME = "testapp"

$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
$port = $env:OPENSHIFT_MSSQL_DB_PORT
$instanceName = "Instance${port}"
$instanceDir = (join-path $currentDir "MSSQL10_50.${InstanceName}")
$dbName = $env:OPENSHIFT_APP_NAME
$username = "sa"

$passwordFile = (Join-Path $instanceDir 'sqlpasswd')
$sysadminPassword = [IO.File]::ReadAllText( $passwordFile )

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

& reg import $registryFile /reg:64

# Start the new instance
$process = (start-process (Join-Path $instanceDir "mssql\binn\sqlservr.exe") "-c -s ${instanceName}" -Passthru -NoNewWindow)

$sqlcmd = (get-command sqlcmd).path

while ((Start-Process -FilePath $sqlcmd -ArgumentList "-Q ""EXEC sp_databases"" -U sa -P ${sysadminPassword} -S ""tcp:127.0.0.1,${port}""" -Wait -Passthru -NoNewWindow).ExitCode -ne 0)
{
    Start-Sleep -s 1
}

start-process -FilePath $sqlcmd "-Q ""CREATE DATABASE [${dbName}]"" -U sa -P ${sysadminPassword} -S ""tcp:127.0.0.1,${port}"""  -Passthru -Wait -NoNewWindow

$process.WaitForExit()