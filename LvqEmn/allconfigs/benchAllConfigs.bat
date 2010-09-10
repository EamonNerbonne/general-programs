REM @echo off

set benchHelper1=%~dp0\benchOneConfig.bat

call "%benchHelper1%" %1 %2 NV %3
call "%benchHelper1%" %1 %2 V %3
