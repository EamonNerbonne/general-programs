@ECHO OFF
REM g++ -I "C:\Program Files (Custom)\boost_1_41_0" -I "C:\Program Files (Custom)\eigen2" -msse2 -O2 -DNDEBUG -DEIGENV2 -o benchV2 DoTest.cpp learningBench.cpp main.cpp mulBench.cpp 
REM -floop-interchange -floop-strip-mine -floop-block

REM C:\MinGW-TDM\bin\

g++ -I "C:\Program Files (Custom)\boost_1_41_0" -I "C:\Program Files (Custom)\eigen2" -march=core2 -m64 -O2 -DEIGEN_DONT_VECTORIZE  -DNDEBUG   -o benchV2  DoTest.cpp learningBench.cpp  mulBench.cpp main.cpp

REM -DEIGEN_DONT_VECTORIZE
REM -march=core2     
REM -maccumulate-outgoing-args -msahf   -fomit-frame-pointer -ftracer 
REM -floop-interchange -floop-strip-mine -floop-block 