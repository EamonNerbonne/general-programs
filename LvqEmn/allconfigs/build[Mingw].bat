@echo off
mkdir %~dp0\..\build\mingw
cd %~dp0\..\build\mingw

REM start /belownormal /wait /B
cmake -G "MinGW Makefiles" ..\..\allconfigs
REM start "MsBuild" /low /B /wait 
start /low /B /wait mingw32-make --jobs 4
IF ERRORLEVEL 1 (
	echo buildFailed > "%~dp0\_BuildFailed-mingw"
) ELSE (
	del "%~dp0\_BuildFailed-mingw"
	IF EXIST "%~1" (
		"%~1" %2 %3 %4 %5 %6 %7 %8 %9
	)
)