@ECHO OFF
REM g++ -I "C:\Program Files (Custom)\boost_1_41_0" -I "C:\Program Files (Custom)\eigen2" -msse2 -O2 -DNDEBUG -DEIGENV2 -o benchV2 DoTest.cpp learningBench.cpp main.cpp mulBench.cpp 
REM -floop-interchange -floop-strip-mine -floop-block

REM C:\MinGW-TDM\bin\

g++  -I "C:\Program Files (Custom)\boost_1_41_0" -I "C:\Program Files (Custom)\eigen2" -I "..\LVQCppNative" -O3 -march=native -mtune=native  -m64 -fomit-frame-pointer -DNDEBUG -o CommandLinePerfTest.exe CommandLinePerfTest.cpp EasyLvqTest.cpp stdafx.cpp libLibLvq2v.a

REM -DEIGEN_DONT_VECTORIZE
REM -march=core2     
REM -maccumulate-outgoing-args -msahf   -fomit-frame-pointer -ftracer 
REM -floop-interchange -floop-strip-mine -floop-block 
