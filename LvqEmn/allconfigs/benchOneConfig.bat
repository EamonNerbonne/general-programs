@echo off

set buildVariant=build%3
set benchExe=%buildVariant%\%1\%2\%1%3.exe

echo %buildVariant%
echo =====%buildVariant%======>>%4
%benchExe%
%benchExe% 2>>%4
%benchExe% 2>>%4
%benchExe% 2>>%4

