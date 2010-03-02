@echo off

set buildVariant=build%3
set benchExe=%buildVariant%\%1\%2\%1%3.exe

echo %buildVariant%
%benchExe% >>%4

