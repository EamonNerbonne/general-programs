@echo off
start "Mingw Build" /low /B /wait cmd /C %~dp0\buildWithMingw.bat
start "Msc Build" /low /B /wait cmd /C %~dp0\buildWithMsc.bat

start "Mingw Lvq Bench" /abovenormal /min cmd /C %~dp0\lvqBenchMingw.bat
start "Msc Lvq Bench" /abovenormal /min cmd /C %~dp0\lvqBenchMsc.bat


