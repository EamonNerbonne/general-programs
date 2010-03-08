@echo off

set buildVariant=build%3
set benchExe=%2\%1%3.exe

echo %buildVariant%
cd %buildVariant%\%1\
start /high /B /wait %benchExe% >>%4
cd ..\..\

