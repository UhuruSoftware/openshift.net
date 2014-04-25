@echo off
echo Assuming msbuild is here: %WINDIR%\Microsoft.NET\Framework\v4.0.30319

echo Checking visual studio native tools path
set vcvar="C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\vcvarsall.bat"

if exist %vcvar% call %vcvar%

set msBuildDir=%WINDIR%\Microsoft.NET\Framework\v4.0.30319

:: Clean output dir first

echo Cleaning .NET assemblies ...
call %msBuildDir%\msbuild.exe /t:Clean %~dp0\node\src\openshift-dotnet\openshift-dotnet.sln /p:Configuration=Release /p:Platform="x64"

if %errorlevel% neq 0 goto build_error

echo Cleaning cartridges ...
call %msBuildDir%\msbuild.exe /t:Clean %~dp0\cartridges\cartridges.sln /p:Configuration=Release /p:Platform="x64"

if %errorlevel% neq 0 goto build_error

:: Build

echo Building .NET assemblies ...
call %msBuildDir%\msbuild.exe %~dp0\node\src\openshift-dotnet\openshift-dotnet.sln /p:Configuration=Release /p:Platform="x64"

if %errorlevel% neq 0 goto build_error

echo Building cartridges ...
call %msBuildDir%\msbuild.exe %~dp0\cartridges\cartridges.sln /p:Configuration=Release /p:Platform="x64"

if %errorlevel% neq 0 goto build_error

set msBuildDir=

exit /b 0

:build_error
echo Build failed
exit /b 1
