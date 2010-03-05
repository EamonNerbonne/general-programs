@echo off
mkdir %~dp0\..\build\mingw
cd %~dp0\..\build\mingw

REM start /belownormal /wait /B
cmake -G "MinGW Makefiles" ..\..\allconfigs
mingw32-make -j
