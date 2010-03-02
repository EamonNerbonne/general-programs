@echo off
mkdir %~dp0\..\build\msc
cd %~dp0\..\build\msc


set benchHelper=%~dp0\benchAllConfigs.bat
set logtarget=%~dp0\bench-msc.log

echo MS COMPILER: >> %logtarget%
date /t >>%logtarget%
time /t >> %logtarget%
echo. >>%logtarget%

call %benchHelper% EigenBench Release %logtarget%
