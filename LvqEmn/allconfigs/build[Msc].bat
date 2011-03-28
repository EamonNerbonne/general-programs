@echo off
mkdir %~dp0\..\build\msc
cd %~dp0\..\build\msc

cmake -G "Visual Studio 10 Win64" ..\..\allconfigs
REM start "MsBuild" /low  /B /wait 
timethis "%~dp0\build[Msc]Helper.bat" %1 %2 %3 %4 %5 %6 %7 %8 %9 >>%~dp0\_BuildTime-msc.log
