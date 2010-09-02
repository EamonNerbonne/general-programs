REM @echo off

set buildVariant=build%~3

IF "%~2" EQU "msc" (
set benchExe=Release\%~1%~3.exe
) else (
set benchExe=%~1%~3.exe
)
set appName=%~1-%~2

echo %buildVariant%
cd "%~dp0\..\build\%~2\%buildVariant%\%1\"
IF "%4" NEQ "" (
echo %buildVariant%: >>"%~dp0\_%appName%.log"
echo %buildVariant%: >>"%~dp0\_%appName%.errlog"
%benchExe% >>"%~dp0\_%appName%.log" 2>>"%~dp0\_%appName%.errlog"
) ELSE (
start /high /B /wait %benchExe% >>"%~dp0\_%appName%.log" 
)
IF ERRORLEVEL 1 (
	echo. > "%~dp0\__FAILING-%appName%-%~3"
) ELSE (
	del "%~dp0\__FAILING-%appName%-%~3"
)


cd "%~dp0"

