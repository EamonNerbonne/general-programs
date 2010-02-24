cmake_minimum_required(VERSION 2.8)
project (LvqBench)

if(NOT build_suffix)
   message(FATAL_ERROR"Build suffix not set!  Have you cmake-ed the outer directory?")
endif()

set(CMAKE_INCLUDE_CURRENT_DIR ON) 

include_directories(../LvqLib)

set(sources EasyLvqTest.cpp LvqBench.cpp)

set(exeName "LvqBench${build_suffix}")
add_executable(${exeName}  ${sources})
target_link_libraries(${exeName} "LvqLib${build_suffix}")
