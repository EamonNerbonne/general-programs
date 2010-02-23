cmake_minimum_required(VERSION 2.8)
project (CommandLinePerfTest)

if(NOT build_suffix)
   message(FATAL_ERROR"Build suffix not set!  Have you cmake-ed the outer directory?")
endif()

set(CommandLinePerfTest_VERSION_MAJOR 0)
set(CommandLinePerfTest_VERSION_MINOR 5)

set(CMAKE_INCLUDE_CURRENT_DIR ON) 

include_directories(../LVQCppNative)

set(sources EasyLvqTest.cpp stdafx.cpp CommandLinePerfTest.cpp)

set(exeName "LvqPerfTest${build_suffix}")
add_executable(${exeName}  ${sources})
target_link_libraries(${exeName} "LibLvq${build_suffix}")
