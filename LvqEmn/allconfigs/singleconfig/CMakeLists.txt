cmake_minimum_required(VERSION 2.8)
project(LvqEamon)

include("optimization-options.cmake")
include("choose-eigen-variant.cmake")

include_directories($ENV{BOOST_DIR})
message("Using Boost dir: $ENV{BOOST_DIR}")

set(solution_dir "../../")
function(build_project Name)
  add_subdirectory("${solution_dir}${Name}" "${Name}")
endfunction(build_project)

#build_project(EigenBench)
build_project(LvqLib)
build_project(LvqBench)
build_project(LvqLibTest)
