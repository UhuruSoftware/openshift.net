$currentDir = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent
Import-Module (Join-Path $currentDir '..\common\openshift-common.psd1') -DisableNameChecking

$json = ConvertFrom-Json -InputObject $args[0]

$porcelain = $json.'--porcelain' -as [bool]
$descriptors = $json.'--with-descriptors' -as [bool]

$output = OO-Cartridge-List -Porcelain $porcelain -WithDescriptors $descriptors -CartName $json.'--cart-name'
write-Output $output.Output
exit $output.ExitCode