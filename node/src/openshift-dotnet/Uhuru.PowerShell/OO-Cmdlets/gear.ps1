$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking

$params = @()
$command = $args[0]

switch ($command.ToString().ToLower())
{
    "deploy" {
        $params += "-Deploy"
        if ($args.Contains("--hot-deploy"))
        {
            $params += "-HotDeploy"
        }
        if ($args.Contains("--force-clean-build"))
        {
            $params += "-ForceCleanBuild"
        }
        $params += "-DeployRefId"
        $params += $args[1]
    }
    default {
        foreach($str in $args)
        {   
            $val = $str
            if ($val.ToString().StartsWith('--'))
            {
                $val = [Regex]::Replace($str, '^--\w',
                    {
                        param($m)
                        $m.ToString().ToUpper().Replace("--", "-")
                    }
                    )
                $val = [Regex]::Replace($val, '-\w',
                    {
                        param($m)
                        $m.ToString().ToUpper().Replace('-', '')
                    }
                    )
                $val = '-' + $val
            }
            else
            {
                if ($params.Length -eq 0)
                {
                    $val = "-${str}"
                }
            }    
            $params += $val
        }
    }
}

$parameters = [string]::Join(' ', $params)
$status = Invoke-Expression "OO-Gear $parameters"
write-Output $status.Output
exit $status.ExitCode