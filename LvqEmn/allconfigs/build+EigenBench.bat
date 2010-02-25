@echo off
call %~dp0\buildWithMingw.bat
call %~dp0\buildWithMsc.bat
call %~dp0\eigenBenchMingw.bat
call %~dp0\eigenBenchMsc.bat
