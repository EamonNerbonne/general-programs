@echo off
mkdir %~dp0\..\build\mingw
cd %~dp0\..\build\mingw

cmake -G "MinGW Makefiles" ..\..\allconfigs
mingw32-make -j  LvqBench3  LvqBench3v

start "Mingw Lvq Bench" /abovenormal /min cmd /C "%~dp0\lvq[Mingw].bat"