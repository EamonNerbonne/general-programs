@echo off
mkdir %~dp0\..\build\mingw
cd %~dp0\..\build\mingw

start /belownormal /wait /B cmake -G "MinGW Makefiles" ..\..\allconfigs
start /belownormal /wait /B mingw32-make -j
