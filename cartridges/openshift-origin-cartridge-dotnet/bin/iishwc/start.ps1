$vhost = $env:OPENSHIFT_APP_DNS

$script:scriptPath = Join-Path (Join-Path $env:OPENSHIFT_DOTNET_DIR bin) iishwc
$script:appPoolName = [Guid]::NewGuid().ToString()
$script:appPort = $env:OPENSHIFT_DOTNET_PORT
$script:appPath = $env:OPENSHIFT_REPO_DIR
$script:logsDir = Join-Path $env:OPENSHIFT_REPO_DIR logs
$script:exitCode = 0

function DetectBitness()
{
    Write-Host("Detecting application bitness...");
    $assemblies = @(Get-ChildItem -Path $script:appPath -Filter "*.dll" -Recurse)
    foreach ($assembly in $assemblies)
    {
        $kind = new-object Reflection.PortableExecutableKinds
        $machine = new-object Reflection.ImageFileMachine
        try
        {
            $a = [Reflection.Assembly]::ReflectionOnlyLoadFrom($assembly.Fullname)
            $a.ManifestModule.GetPEKind([ref]$kind, [ref]$machine)
        }
        catch
        {
            Write-Error("Could not detect bitness for assembly $($assembly.Name)");
            $kind = [System.Reflection.PortableExecutableKinds]"NotAPortableExecutableImage"
        }
        
        switch ($kind)
        {
            [System.Reflection.PortableExecutableKinds]::Required32Bit
            {
                Write-Host("Application requires a 32bit enabled application pool");
                return ,$true;                
            }			
            ([System.Reflection.PortableExecutableKinds]([System.Reflection.PortableExecutableKinds]::Required32Bit -bor [System.Reflection.PortableExecutableKinds]::ILOnly))
            {
                Write-Host("Application requires a 32bit enabled application pool");
                return ,$true;
            }
            default { }
        }			  		
    }
    
    Write-Host("Application does not require a 32bit enabled application pool");
    return $false
}

function GetFrameworkFromConfig()
{
    Write-Host("Detecting required asp.net version...");
    $webConfigPath = Join-Path $script:appPath 'web.config'
    $webConfig = New-Object System.Xml.XmlDocument
    $webConfig.Load($webConfigPath)

    $node = $webConfig.SelectSingleNode("/configuration/system.web/compilation/@targetFramework")
    if ($node)
    {
        Write-Host("Application requires asp.net v4.0");
        return "v4.0"
    }
    else
    {
        Write-Host("Application requires asp.net v2.0");
        return "v2.0"
    }
}

function AddApplicationPool([ref]$applicationHost)
{
    Write-Output("Creating application pool in applicationHost.config")
    $enable32bit = DetectBitness
    if ($enable32bit -eq $true)
    {
        $script:exitCode = 1
    }

    $defaults = $applicationHost.Value.SelectSingleNode("/configuration/system.applicationHost/applicationPools/applicationPoolDefaults/processModel")
    $defaults.SetAttribute("identityType", "SpecificUser")

    $element = $applicationHost.Value.CreateElement("add")
    $element.SetAttribute('name', $script:appPoolName)
    $element.SetAttribute('enable32BitAppOnWin64', $enable32bit)
    $framework = GetFrameworkFromConfig
    $element.SetAttribute('managedRuntimeVersion', $framework)
    $null = $applicationHost.Value.configuration."system.applicationHost".applicationPools.AppendChild($element)
}

function AddSite([ref]$applicationHost, $appName)
{
    Write-Output("Creating site in applicationHost.config")
    $appName = [Guid]::NewGuid().ToString()
    $element = $applicationHost.Value.CreateElement("site")
    $element.SetAttribute('name', $appName)
    $element.SetAttribute('id', 1)
    $null = $applicationHost.Value.configuration."system.applicationHost".sites.AppendChild($element)
}

function AddBinding([ref]$applicationHost)
{
    Write-Output("Adding http bindings for application")
    $bindings = $applicationHost.Value.CreateElement("bindings")	
    $element = $applicationHost.Value.CreateElement("binding")
    $element.SetAttribute('protocol', "http")
    $element.SetAttribute("bindingInformation", [String]::Format("*:{0}:{1}", $script:appPort, "*"))	
    $null = $bindings.AppendChild($element)
    $null = $applicationHost.Value.configuration."system.applicationHost".sites.site.AppendChild($bindings)	
}

function AddApplication([ref]$applicationHost)
{
    Write-Output("Adding application and virtual directory to site")
    $application = $applicationHost.Value.CreateElement("application")
    $application.SetAttribute('path', '/')
    $application.SetAttribute('applicationPool', $script:appPoolName)
    $virtualDirectory = $applicationHost.Value.CreateElement("virtualDirectory")
    $virtualDirectory.SetAttribute('path', '/')
    $virtualDirectory.SetAttribute('physicalPath', $script:appPath)
    $null = $application.AppendChild($virtualDirectory)
    $null = $applicationHost.Value.configuration."system.applicationHost".sites.site.AppendChild($application)	
}

$applicationHostTemplatePath = Join-Path $script:scriptPath 'applicationHostTemplate.config'
$applicationHostPath = Join-Path $script:scriptPath 'applicationHost.config'
Copy-Item -Force $applicationHostTemplatePath $applicationHostPath

$applicationHost = New-Object System.Xml.XmlDocument
$applicationHost.Load($applicationHostPath)

AddApplicationPool ([ref]$applicationHost)
AddSite ([ref]$applicationHost)
AddBinding ([ref]$applicationHost)
AddApplication ([ref]$applicationHost)

$applicationHost.Save($applicationHostPath)

$webConfigPath = Join-Path $script:appPath "web.config"
$webConfig = New-Object System.Xml.XmlDocument
$webConfig.Load($webConfigPath)

$appSettings = $webConfig.SelectSingleNode("/configuration/appSettings")
if($appSettings -eq $null)
{
    $appSettings = $webConfig.CreateElement("appSettings")
    $configuration = $webConfig.SelectSingleNode("/configuration")
    $null = $configuration.AppendChild($appSettings)
}
$element = $webConfig.CreateElement("add")
$element.SetAttribute('key', "UHURU_LOG_FILE")
$element.SetAttribute('value', (Join-Path $script:logsDir iis.stdout.log))
$null = $appSettings.AppendChild($element)
$element = $webConfig.CreateElement("add")
$element.SetAttribute('key', "UHURU_ERROR_LOG_FILE")
$element.SetAttribute('value', (Join-Path $script:logsDir iis.stderr.log))
$null = $appSettings.AppendChild($element)

#$healthMonitoring = $webConfig.SelectSingleNode("/configuration/system.web/healthMonitoring")
#if($healthMonitoring -eq $null)
#{
#    $healthMonitoring = $webConfig.CreateElement("healthMonitoring")
#    $systemWeb = $webConfig.SelectSingleNode("/configuration/system.web")
#    $healthMonitoring.SetAttribute("configSource", "UhuruAspNetEventProvider.config")
#    $null = $systemWeb.AppendChild($healthMonitoring)
#}

$webConfig.Save($webConfigPath)

# New-Item -Path (Join-Path $env:OPENSHIFT_DOTNET_DIR '\tmp\IIS Temporary Compressed Files') -Force -ErrorAction SilentlyContinue -Type Directory | Out-Null

Write-Output("Starting IIS Process")

$host.SetShouldExit($script:exitCode)
exit $script:exitCode
