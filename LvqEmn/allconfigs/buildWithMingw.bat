@echo off
mkdir %~dp0\..\build\mingw
cd %~dp0\..\build\mingw

cmake -G "MinGW Makefiles" ..\..\allconfigs
make -j
set benchHelper=%~dp0\benchAllConfigs.bat

set logtarget=%~dp0\bench-mingw.log
echo ===============================================MINGW COMPILER!!!!!!!!!! >> %logtarget%
date /t >>%logtarget%
time /t >> %logtarget%
echo. >>%logtarget%

call %benchHelper% EigenBench . %logtarget%
