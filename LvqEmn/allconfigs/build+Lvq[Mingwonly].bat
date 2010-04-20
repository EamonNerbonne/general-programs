@echo off
mkdir %~dp0\..\build\mingw
cd %~dp0\..\build\mingw

cmake -G "MinGW Makefiles" ..\..\allconfigs
mingw32-make -j LvqBench2 LvqBench3 LvqBench2v LvqBench3v

call "..\..\allconfigs\lvq[Mingw].bat"
