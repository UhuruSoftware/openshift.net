$port = $env:OPENSHIFT_DOTNET_PORT
$path = $env:OPENSHIFT_REPO_DIR + "\dotnet\"

#$port = 8080
#$path = "D:\_code\openshift.net\src\cartridges\openshift-origin-cartridge-dotnet\usr\template\dotnet\"

Write-Host "Starting a web server on port $port ..."

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://+:$port/")

$listener.Start()
$stop = 0

Write-Host 'Listening ...'
try
{
    #while (($listener.IsListening) -and ($stop -eq 0)) {
    #while ($stop -eq 0) {
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

            $response.ContentLength64 = $msg.Length

        try
        {
            $stream = $response.OutputStream
            $stream.Write($msg, 0, $msg.Length)
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



