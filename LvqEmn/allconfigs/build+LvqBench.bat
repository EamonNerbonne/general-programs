REM @echo off
start "Mingw Build" /low /B /wait cmd /C "%~dp0\build[Mingw].bat"
start "Msc Build" /low /B /wait cmd /C "%~dp0\build[Msc].bat"

start "Mingw Lvq Bench" /abovenormal /min cmd /C "%~dp0\LvqBench[Mingw].bat"
start "Msc Lvq Bench" /abovenormal /min cmd /C "%~dp0\LvqBench[Msc].bat"


