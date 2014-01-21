

function Run-RubyCommand($rubyDir, $command, $directory)
{
    $scriptLocation = [System.IO.Path]::GetTempFileName() + '.bat'

    Write-Template (Join-Path $currentDir "rubycmd.bat.template") $scriptLocation @{
        rubyPath = (Join-Path $rubyDir 'bin')
        directory = $directory
        command = $command
    }

    $rubyProcess = Start-Process -Wait -PassThru -NoNewWindow 'cmd' "/c ${scriptLocation}"

    if ($rubyProcess.ExitCode -ne 0)
    {
        Write-Error "Error running ruby command '${command}'. Please check install logs."
        exit 1
    }
    else
    {
        Write-Host "[OK] Ruby command '${command}' ran successfully."
    }
}
