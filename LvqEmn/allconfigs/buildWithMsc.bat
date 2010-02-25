@echo off
mkdir %~dp0\..\build\msc
cd %~dp0\..\build\msc

cmake -G "Visual Studio 10 Win64" ..\..\allconfigs
"C:\Windows\Microsoft.NET\Framework64\v4.0.30128\MSBuild.exe" /property:Configuration=Release /maxcpucount ALL_BUILD.vcxproj
set benchHelper=%~dp0\benchAllConfigs.bat

set logtarget=%~dp0\bench-msc.log
echo ===============================================MICROSOFT COMPILER!!!!!!!!!! >> %logtarget%
date /t >>%logtarget%
time /t >> %logtarget%
echo. >>%logtarget%

call %benchHelper% EigenBench Release %logtarget%
