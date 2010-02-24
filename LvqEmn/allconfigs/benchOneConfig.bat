@echo off

set buildVariant=build%3
set benchExe=%buildVariant%\%1\%2\%1%3.exe

echo %buildVariant%
echo =====%buildVariant%======>>benchlog.log
%benchExe%
%benchExe% >>%4
%benchExe% >>%4
%benchExe% >>%4

