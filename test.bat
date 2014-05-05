@echo off
echo Checking visual studio native tools path
set vcvar="C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\vcvarsall.bat"

if exist %vcvar% call %vcvar%

mkdir TestResults
rd /S /q TestResults
vstest.console.exe node\src\openshift-dotnet\Uhuru.Openshift.Tests\bin\Uhuru.Openshift.Tests.dll /Settings:node\src\openshift-dotnet\Uhuru.Openshift.Tests\local.runsettings /Logger:trx
ren "TestResults\*.trx" openshiftdotnet.trx