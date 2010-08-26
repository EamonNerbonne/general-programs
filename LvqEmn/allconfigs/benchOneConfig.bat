@echo off

set buildVariant=build%~3
set benchExe=%~2\%~1%~3.exe

echo %buildVariant%
cd %buildVariant%\%1\
IF "%5" NEQ "" (
echo %buildVariant%: >>"%~4"
echo %buildVariant%: >>"%~5"
start /high /B /wait %benchExe% >>"%~4" 2>>"%~5"
)
IF "%5" EQU "" (
start /high /B /wait %benchExe% >>"%~4" 
)

cd ..\..\

