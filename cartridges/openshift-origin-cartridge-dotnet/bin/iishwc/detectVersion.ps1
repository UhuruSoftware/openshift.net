$script:scriptPath = Join-Path (Join-Path $env:OPENSHIFT_DOTNET_DIR bin) iishwc
$script:appPath = $env:OPENSHIFT_REPO_DIR

$applicationHostPath = Join-Path $script:scriptPath 'applicationHost.config'
$webConfig = New-Object System.Xml.XmlDocument
$webConfig.Load($applicationHostPath)
$val = $webConfig.SelectSingleNode("/configuration/system.applicationHost/applicationPools/add").Attributes["enable32BitAppOnWin64"].Value.ToString()
$enabled32bit = [System.Convert]::ToBoolean($val)

if ($enabled32bit)
{
    $bitness = "86"
}
else
{
    $bitness = "64"
}

$webConfigPath = Join-Path $script:appPath 'web.config'
$webConfig = New-Object System.Xml.XmlDocument
$webConfig.Load($webConfigPath)

$node = $webConfig.SelectSingleNode("/configuration/system.web/compilation/@targetFramework")
if ($node)
{
    Write-Host("Application requires asp.net v4.0");
    $version = 40
}
else
{
    Write-Host("Application requires asp.net v2.0");
    $version = 20
}

$rootWebConfigFileName = "rootWeb" + $version + $bitness + ".config"
$rootWebConfigPath = Join-Path $script:scriptPath $rootWebConfigFileName

$rootWebConfig = New-Object System.Xml.XmlDocument
$rootWebConfig.Load($rootWebConfigPath)
$tmpPath = Join-Path $env:OPENSHIFT_REPO_DIR "tmp"
$compilationPath = Join-Path $tmpPath "aspnet_compilation"

$element = $rootWebConfig.SelectSingleNode("configuration/system.web/compilation")
$element.SetAttribute('tempDirectory', $compilationPath)

$rootWebConfig.Save($rootWebConfigPath)

$host.SetShouldExit($version)
exit $version