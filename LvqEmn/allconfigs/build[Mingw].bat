@echo off
mkdir %~dp0\..\build\mingw
cd %~dp0\..\build\mingw

REM start /belownormal /wait /B
cmake -G "MinGW Makefiles" ..\..\allconfigs
timethis "%~dp0\build[Mingw]Helper.bat"  %1 %2 %3 %4 %5 %6 %7 %8 %9 >>%~dp0\_BuildTime-mingw.log
