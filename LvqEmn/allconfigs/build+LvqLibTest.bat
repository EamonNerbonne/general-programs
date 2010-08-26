REM @echo off
start "Mingw Build" /low /B /wait cmd /C "%~dp0\build[Mingw].bat"
start "Msc Build" /low /B /wait cmd /C "%~dp0\build[Msc].bat"

set benchHelper=%~dp0\benchAllConfigs.bat

mkdir "%~dp0\..\build\msc"
cd "%~dp0\..\build\msc"
call "%benchHelper%" LvqLibTest Release "%~dp0\test-msc.log" "%~dp0\test-msc.report"

mkdir "%~dp0\..\build\mingw"
cd "%~dp0\..\build\mingw"
call "%benchHelper%" LvqLibTest . "%~dp0\test-mingw.log" "%~dp0\test-mingw.report"

