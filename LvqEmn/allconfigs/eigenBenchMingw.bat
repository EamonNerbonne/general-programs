@echo off
mkdir %~dp0\..\build\mingw
cd %~dp0\..\build\mingw

set benchHelper=%~dp0\benchAllConfigs.bat
set logtarget=%~dp0\bench.log
call %benchHelper% EigenBench . %logtarget%
