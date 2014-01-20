@echo off
echo Assuming msbuild is here: %WINDIR%\Microsoft.NET\Framework\v4.0.30319

set msBuildDir=%WINDIR%\Microsoft.NET\Framework\v4.0.30319

:: Clean output dir first

echo Cleaning .NET assemblies ...
call %msBuildDir%\msbuild.exe /t:Clean %~dp0\node\src\openshift-dotnet\openshift-dotnet.sln

echo Cleaning cartridges ...
call %msBuildDir%\msbuild.exe /t:Clean %~dp0\cartridges\cartridges.sln


:: Build

echo Building .NET assemblies ...
call %msBuildDir%\msbuild.exe %~dp0\node\src\openshift-dotnet\openshift-dotnet.sln

echo Building cartridges ...
call %msBuildDir%\msbuild.exe %~dp0\cartridges\cartridges.sln


set msBuildDir=
