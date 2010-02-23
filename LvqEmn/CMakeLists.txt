cmake_minimum_required(VERSION 2.8)
project(LvqEamon)

set(CMAKE_BUILD_TYPE "Release" CACHE STRING "Release or Debug")
set(EigenVersion 2 CACHE STRING "Which eigen version to use: 2 or 3")
set(EigenVectorize ON CACHE BOOL "Use eigen's auto-vectorization")

message("Using build type ${CMAKE_BUILD_TYPE}")

if(CMAKE_COMPILER_IS_GNUCXX)
	message("Compiler is Gcc")
	set(CMAKE_CXX_FLAGS_RELEASE "-m64 -O2 -march=native -mtune=native  -fomit-frame-pointer -maccumulate-outgoing-args -msahf   -fomit-frame-pointer -ftracer -DNDEBUG")
	set(CMAKE_CSS_FLAGS_DEBUG "-g3")
	set(INCL_FLAG "-I")
elseif(MSVC)
	message("Compiler is msvc")
	set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /EHa") #SEH exceptions
	set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /arch:SSE2") #SSE2 support
	set(CMAKE_CXX_FLAGS_RELEASE " /Ox /Ot /GL /fp:fast -DNDEBUG")
	set(INCL_FLAG "/I")
else()
	message(FATAL_ERROR "Unrecognized compiler!")
endif()

include_directories("C:/Program Files (Custom)/boost_1_41_0")

#build options:
  if(EigenVersion EQUAL 3)
    message("Using Eigen v3")
    add_definitions(-DEIGEN3)
    include_directories("C:/Program Files (Custom)/eigen")
  elseif(EigenVersion EQUAL 2)
    message("Using Eigen v2")
    add_definitions(-DEIGEN2)
    include_directories("C:/Program Files (Custom)/eigen2")
  else()
    message(FATAL_ERROR "Invalid Eigen version.")
  endif()
  
  set(build_suffix ${EigenVersion})
  if(EigenVectorize)
  	set(build_suffix "${build_suffix}v")
    remove_definitions(-DEIGEN_DONT_VECTORIZE)
  else()
    add_definitions(-DEIGEN_DONT_VECTORIZE)
  endif()
#end build options


add_subdirectory("EigenBench")

add_subdirectory("LVQCppNative")
add_subdirectory("CommandLinePerfTest")