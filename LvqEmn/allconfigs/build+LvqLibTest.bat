REM @echo off
start "Mingw Build" /low /B /wait cmd /C "%~dp0\build[Mingw].bat"
start "Msc Build" /low /B /wait cmd /C "%~dp0\build[Msc].bat"

set benchHelper=%~dp0\benchAllConfigs.bat

call "%benchHelper%" LvqLibTest msc errlog
call "%benchHelper%" LvqLibTest mingw errlog
