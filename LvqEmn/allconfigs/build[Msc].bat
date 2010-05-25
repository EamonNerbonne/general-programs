@echo off
mkdir %~dp0\..\build\msc
cd %~dp0\..\build\msc

cmake -G "Visual Studio 10 Win64" ..\..\allconfigs
start "MsBuild" /low  /B /wait C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe /property:Configuration=Release /maxcpucount "ALL_BUILD.vcxproj"
