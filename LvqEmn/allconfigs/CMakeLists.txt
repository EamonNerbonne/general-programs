cmake_minimum_required(VERSION 2.8)
project(LvqEamonAllconfigs)

message("Using compiler at ${CMAKE_CXX_COMPILER}")

set(EigenVersion 3)
set(EigenVectorize OFF)
add_subdirectory("singleconfig" "buildNV")

set(EigenVersion 3)
set(EigenVectorize ON)
add_subdirectory("singleconfig" "buildV")
