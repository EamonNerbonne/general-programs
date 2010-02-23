cmake_minimum_required(VERSION 2.8)
project(EigenBench)

if(NOT build_suffix)
   message(FATAL_ERROR"Build suffix not set!  Have you cmake-ed the outer directory?")
endif()

set(exeName "BenchEigen${build_suffix}")
set(sources DoTest.cpp learningBench.cpp  mulBench.cpp main.cpp)
add_executable(${exeName}  ${sources})
