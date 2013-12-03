$port = $env:OPENSHIFT_DOTNET_PORT
$path = $env:OPENSHIFT_REPO_DIR + "\dotnet\"
$vhost = $env:OPENSHIFT_APP_DNS

Write-Host "Starting a web server on port $port ..."

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://${vhost}:${port}/")

$listener.Start()
$stop = 0

Write-Host 'Listening ...'
try
{
    while (Test-Path  $env:HTTPD_PID_FILE) {
        $context = $listener.GetContext()
        $request = $context.Request
        $response = $context.Response

        $currentTime = date
    
        $filename = $request.Url.AbsolutePath

        Write-Host "[${currentTime}] REQUEST '${filename}'"

        if ($filename -eq '/') { $filename = 'index.html' }

        $filename = Join-Path $path $filename

        Write-Host "[${currentTime}] RESPONSE '${filename}'"

        if ((Test-Path $filename) -ne $true)
        {
            $response.StatusCode = [System.Net.HttpStatusCode]::NotFound
            $msg = [System.Text.ASCIIEncoding]::ASCII.GetBytes('<h1>Not Found</h1>')
        }
        else
        {
            $msg = Get-Content $filename -Encoding byte
        }

        $envs = (Get-ChildItem Env: | ForEach-Object { $_.name + "---------------" + $_.value })
        $msgEnv = [System.Text.ASCIIEncoding]::ASCII.GetBytes([string]::Join("<br/>", $envs))

        $response.ContentLength64 = $msg.Length + $msgEnv.Length

        try
        {
            $stream = $response.OutputStream
            $stream.Write($msg, 0, $msg.Length)
            $stream.Write($msgEnv, 0, $msgEnv.Length)
        }
        catch
        {
            # TODO (vladi): we should log to a location based on openshift's best practices
            $_.Exception.Message | Out-File (Join-Path $path "log.html")
        }
        finally 
        {
            $stream.Dispose()
        }
    }
}

finally
{
    Write-Host "Exiting..." 
    $listener.Close()
    $listener.Dispose()
}



