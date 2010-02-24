@echo off
mkdir %~dp0\..\build\mingw
cd %~dp0\..\build\mingw

cmake -G "MinGW Makefiles" ..\..\allconfigs
make -j
set benchHelper=%~dp0\benchAllConfigs.bat

echo. >>benchlog.log
echo ===============================================MINGW COMPILER!!!!!!!!!! >> benchlog.log
call %benchHelper% EigenBench . benchlog.log
