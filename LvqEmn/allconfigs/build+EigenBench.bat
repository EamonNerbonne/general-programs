@echo off
call %~dp0\buildWithMingw.bat
call %~dp0\buildWithMsc.bat
start "Mingw Eigen Bench" /abovenormal /min cmd /C %~dp0\eigenBenchMingw.bat
start "Msc Eigen Bench" /abovenormal /min cmd /C %~dp0\eigenBenchMsc.bat
