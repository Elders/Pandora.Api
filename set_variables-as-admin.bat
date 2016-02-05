echo off
@setlocal enableextensions
cd /d "%~dp0"

SET NUGET=%LocalAppData%\NuGet\NuGet.exe

echo Downloading latest version of NuGet.exe...
IF NOT EXIST %LocalAppData%\NuGet md %LocalAppData%\NuGet
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://www.nuget.org/nuget.exe' -OutFile '%NUGET%'"

echo Downloading latest version of Pandora.Cli.exe...
%NUGET% "install" "Pandora.Cli" "-OutputDirectory" "%LocalAppData%" "-ExcludeVersion" "-Prerelease"

set env=%CLUSTER_NAME%
set host=%COMPUTERNAME%
echo MachineName %host%
setlocal enabledelayedexpansion

@cd /d "%~dp0"\src\Pandora.Server.Api.Configuration
CALL :setVaraiabless Elders.Pandora.Server.Api.json Elders.Pandora.Server.Api

echo

goto :eof
:setVaraiabless
SETLOCAL

set file=%1
set app=%2
echo Calling... Elders.Pandora.exe open -a %app% -c %env% -m %host% -j %file%
%LocalAppData%\Pandora.Cli\tools\Elders.Pandora.Cli.exe open -a %app% -c %env% -m %host% -j %file%
ENDLOCAL
PAUSE