@echo off
mkdir %~dp0\..\build\msc
cd %~dp0\..\build\msc

cmake -G "Visual Studio 10 Win64" ..\..\allconfigs
REM start "MsBuild" /low  /B /wait 
start /low /B /wait C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe /property:Configuration=Release /maxcpucount "ALL_BUILD.vcxproj"
IF ERRORLEVEL 1 (
	echo buildFailed > "%~dp0\_BuildFailed-msc"
) ELSE (
	del "%~dp0\_BuildFailed-msc"
	IF EXIST "%~1" (
		"%~1" %2 %3 %4 %5 %6 %7 %8 %9
	)
)

