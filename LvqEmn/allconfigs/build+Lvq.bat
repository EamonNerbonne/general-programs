@echo off
start /low /wait cmd /C %~dp0\buildWithMingw.bat
start /low /wait cmd /C %~dp0\buildWithMsc.bat

start  cmd /C %~dp0\lvqBenchMingw.bat
start cmd /C %~dp0\lvqBenchMsc.bat


