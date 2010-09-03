cmake_minimum_required(VERSION 2.8)
project (LvqLibTest)

if(NOT build_suffix)
   message(FATAL_ERROR"Build suffix not set!  Have you cmake-ed the outer directory?")
endif()

set(CMAKE_INCLUDE_CURRENT_DIR ON) 

include_directories(../LvqLib)

set(sources driver.cpp TestCovariance.cpp TestPca.cpp RowColAssignments.cpp)

set(exeName "LvqLibTest${build_suffix}")
add_executable(${exeName}  ${sources})
target_link_libraries(${exeName} "LvqLib${build_suffix}")
