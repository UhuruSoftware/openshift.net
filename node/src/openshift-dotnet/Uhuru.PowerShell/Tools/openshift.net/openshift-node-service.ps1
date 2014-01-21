$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent

Import-Module (Join-Path $currentDir '..\..\common\openshift-common.psd1') -DisableNameChecking
. (Join-Path $currentDir 'ruby-helpers.ps1')

$config = [Uhuru.Openshift.Runtime.Config.NodeConfig]::Values


$rubyPath = 'c:\openshift\ruby'
$cygwinPath = 'c:\openshift\cygwin'
$mcollectivePath = 'c:\openshift\mcollective'

function Run-SSHD()
{
    return Invoke-Command -ScriptBlock {
        Write-Host 'Running sshd ...'
        $runSSHDScript = (Join-Path $currentDir '..\sshd\run-sshd.ps1')
        $cygwinInstallationPath = (Join-Path $cygwinPath 'installation')
        $global:sshdProcess = Start-Process -PassThru -NoNewWindow 'powershell' "-File ""${runSSHDScript}"" -targetDirectory ""${cygwinInstallationPath}""" 
        $global:sshdProcess.WaitForExit()
        Stop-MCollective
    } -Computer localhost -AsJob 
}

function Run-MCollective()
{
    return Invoke-Command -ScriptBlock {
        Write-Host 'Running mcollective ...'
        $mcollectiveLib = (Join-Path $mcollectivePath 'lib').Replace("\", "/")
        $mcollectiveBin = (Join-Path $mcollectivePath 'bin\mcollectived')
        $mcollectiveConfig = (Join-Path $mcollectivePath 'etc\server.cfg')

        $mcollectiveRunCommand = "ruby -I""${mcollectiveLib}"" -- ""${mcollectiveBin}"" --config ""${mcollectiveConfig}"""
        $global:mcollectiveProcess = Run-RubyProcess $rubyPath 
        $global:mcollectiveProcess.WaitForExit()
        Stop-SSHD
    } -Computer localhost -AsJob   
}

function Stop-SSHD()
{
    [System.Windows.Forms.SendKeys]::SendWait("+(C)");
}

function Stop-MCollective()
{
    [System.Windows.Forms.SendKeys]::SendWait("+(C)");
}

#[Console]::TreatControlCAsInput = $true

#$completedJobs = (@((Run-SSHD), (Run-MCollective)) | Wait-Job -Timeout 1)

@((Run-SSHD), (Run-MCollective)) | Wait-Job

#while ($true)
#{
#    $completedJobs = (@((Run-SSHD), (Run-MCollective)) | Wait-Job -Timeout 1)

#    if ($completedJobs -ne $null)
#    {
#        exit 0
#    }

#    if ([console]::KeyAvailable)
#    {
#        $key = [Console]::ReadKey($true)
#        if (($key.Modifiers -band [ConsoleModifiers]"control") -and ($key.Key -eq "C"))
#        {
#            "Caught CTRL-C, stopping jobs ..."
#            Stop-MCollective
#            Stop-SSHD            
#        }
#    }
#}