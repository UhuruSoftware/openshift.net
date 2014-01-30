$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking

$json = ConvertFrom-Json -InputObject $args[0]
$config = $json.'--with-config'

$autoDeploy = $config.'auto_deploy' -as [bool]
$keepDeployments = $config.'keep_deployments' -as[int]

$output = OO-Update-Configuration -WithAppUuid $json.'--with-app-uuid' -WithAppName $json.'--with-app-name' -WithContainerUuid $json.'--with-container-uuid' -WithContainerName $json.'--with-container-name' -WithNamespace $json.'--with-namespace' -WithRequestId $json.'--with-request-id' -AutoDeploy $autoDeploy -DeploymentBranch $config.'deployment_branch' -KeepDeployments $keepDeployments -DeploymentType $config.'deployment_type'
write-Output $output.Output
exit $output.ExitCode