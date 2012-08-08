REM @echo off
set OLDDIR=%CD%
set BASEDIR=%~dp0

mkdir %BASEDIR%\..\build\mingw
cd %BASEDIR%\..\build\mingw

REM start /belownormal /wait /B
cmake -G "MinGW Makefiles" "%BASEDIR%."
timethis "%BASEDIR%\build[Mingw]Helper.bat"  %1 %2 %3 %4 %5 %6 %7 %8 %9 >>"%BASEDIR%\_BuildTime-mingw.log"
chdir /d %OLDDIR% &rem restore current directory