@echo off
call %~dp0\build[Mingw].bat
call %~dp0\build[Msc].bat
start "Mingw Eigen Bench" /abovenormal /min cmd /C %~dp0\eigenBenchMingw.bat
start "Msc Eigen Bench" /abovenormal /min cmd /C %~dp0\eigenBenchMsc.bat
