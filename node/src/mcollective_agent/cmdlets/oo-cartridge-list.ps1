$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
$modulePath = Join-Path $scriptPath '..\..\..\bin\Uhuru.Openshift.Cmdlets.dll'
Import-Module $modulePath
$json = ConvertFrom-Json -InputObject $args[0]

$porcelain = $json.'--porcelain' -as [bool]
$descriptors = $json.'--with-descriptors' -as [bool]

$output = Get-CartridgeList -Porcelain $porcelain -WithDescriptors $descriptors -CartName $json.'--cart-name'
write-host $output
exit 0