@echo off
mkdir %~dp0\..\build\msc
cd %~dp0\..\build\msc

set benchHelper=%~dp0\benchAllConfigs.bat
set logtarget=%~dp0\bench-msc.log

call %benchHelper% LvqBench Release %logtarget%
