@ECHO OFF
REM g++ -I "C:\Program Files (Custom)\boost_1_41_0" -I "C:\Program Files (Custom)\eigen" -O2 -msse2 -DNDEBUG   -o benchDev DoTest.cpp learningBench.cpp main.cpp mulBench.cpp 
REM -floop-interchange -floop-strip-mine -floop-block

g++ -I "C:\Program Files (Custom)\boost_1_41_0" -I "C:\Program Files (Custom)\eigen" -march=native -mtune=native -O3 -DEIGEN_DONT_VECTORIZE -DEIGEN3 -DNDEBUG -o benchDev DoTest.cpp learningBench.cpp main.cpp mulBench.cpp 

REM -floop-interchange -floop-strip-mine -floop-block