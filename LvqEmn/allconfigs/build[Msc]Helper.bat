@echo off
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe /property:Configuration=Release /maxcpucount:4 "ALL_BUILD.vcxproj" > %~dp0\_Build-msc.log
IF ERRORLEVEL 1 (
	echo buildFailed > "%~dp0\_BuildFailed-msc"
) ELSE (
	del "%~dp0\_BuildFailed-msc"
	IF EXIST "%~1" (
		"%~1" %2 %3 %4 %5 %6 %7 %8 %9
	)
)

