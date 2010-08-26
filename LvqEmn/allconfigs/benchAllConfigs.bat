REM @echo off

set benchHelper1=%~dp0\benchOneConfig.bat

call %benchHelper1% %1 %2 3 %3
call %benchHelper1% %1 %2 3v %3
