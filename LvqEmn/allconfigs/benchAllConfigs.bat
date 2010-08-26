@echo off

set benchHelper1=%~dp0\benchOneConfig.bat

echo. >> %3
REM call %benchHelper1% %1 %2 2 %3
REM call %benchHelper1% %1 %2 2v %3
call %benchHelper1% %1 %2 3 %3 %4
call %benchHelper1% %1 %2 3v %3 %4
