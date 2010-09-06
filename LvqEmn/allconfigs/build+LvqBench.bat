@echo off
start "Mingw Eigen Build" /min cmd /C ""%~dp0\build[Mingw].bat" "%~dp0\LvqBench[Mingw].bat""
start "Msc Eigen Build"  /min cmd /C ""%~dp0\build[Msc].bat" "%~dp0\LvqBench[Msc].bat""

