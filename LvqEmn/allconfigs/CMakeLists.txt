cmake_minimum_required(VERSION 2.8)
project(LvqEamonAllconfigs)

set(EigenVersion 2)
set(EigenVectorize OFF)
add_subdirectory("singleconfig" "build2")

set(EigenVersion 2)
set(EigenVectorize ON)
add_subdirectory("singleconfig" "build2v")

set(EigenVersion 3)
set(EigenVectorize OFF)
add_subdirectory("singleconfig" "build3")

set(EigenVersion 3)
set(EigenVectorize ON)
add_subdirectory("singleconfig" "build3v")
