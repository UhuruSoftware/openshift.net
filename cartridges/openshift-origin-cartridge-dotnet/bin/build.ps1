$repoDir = $env:OPENSHIFT_REPO_DIR
$msbuildPath = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe'

Get-ChildItem $repoDir\*.* | where {$_.extension -eq ".sln"} | %{
	$msbuildArgs = @($_.FullName, "/t:Rebuild")
	& $msbuildPath $msbuildArgs
}