REM @echo off
REM mingw32-make "MAKE=mingw32-make -j8" SHELL=cmd.exe -j8 >%~dp0\_Build-mingw.log

mingw32-make "MAKE=mingw32-make" SHELL=cmd.exe  >%~dp0\_Build-mingw.log
IF ERRORLEVEL 1 (
	echo buildFailed > "%~dp0\_BuildFailed-mingw"
) ELSE (
	IF EXIST "%~dp0\_BuildFailed-mingw" (del "%~dp0\_BuildFailed-mingw")
	IF EXIST "%~1" (
		"%~1" %2 %3 %4 %5 %6 %7 %8 %9
	)
)