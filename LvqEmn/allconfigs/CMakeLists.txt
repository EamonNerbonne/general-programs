cmake_minimum_required(VERSION 2.8)
project(LvqEamonAllconfigs)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
#set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_SHARED_LIBRARY_PREFIX "")

message("Using compiler at ${CMAKE_CXX_COMPILER}")

set(EigenVersion 3)
set(EigenVectorize OFF)
add_subdirectory("singleconfig" "buildNV")

set(EigenVersion 3)
set(EigenVectorize ON)
add_subdirectory("singleconfig" "buildV")
