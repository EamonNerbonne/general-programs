@echo off
mkdir %~dp0\..\build\msc
cd %~dp0\..\build\msc

start /belownormal /wait /B cmake -G "Visual Studio 10 Win64" ..\..\allconfigs
start /belownormal /wait /B C:\Windows\Microsoft.NET\Framework64\v4.0.30128\MSBuild.exe "/property:Configuration=Release" "/maxcpucount" "ALL_BUILD.vcxproj"
