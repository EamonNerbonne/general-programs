REM @echo off
set OLDDIR=%CD%
set BASEDIR=%~dp0
mkdir %~dp0\..\build\msc
cd %~dp0\..\build\msc

cmake -G "Visual Studio 11 Win64" "%BASEDIR%."
REM start "MsBuild" /low  /B /wait 
timethis "%BASEDIR%\build[Msc]Helper.bat" %1 %2 %3 %4 %5 %6 %7 %8 %9 >>"%BASEDIR%\_BuildTime-msc.log"
chdir /d %OLDDIR% &rem restore current directory
