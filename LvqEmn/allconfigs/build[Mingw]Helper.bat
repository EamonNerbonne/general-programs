@echo off
mingw32-make -j >%~dp0\_Build-mingw.log
IF ERRORLEVEL 1 (
	echo buildFailed > "%~dp0\_BuildFailed-mingw"
) ELSE (
	del "%~dp0\_BuildFailed-mingw"
	IF EXIST "%~1" (
		"%~1" %2 %3 %4 %5 %6 %7 %8 %9
	)
)