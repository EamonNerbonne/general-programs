cmake_minimum_required(VERSION 2.8)
project(LvqEamon)

set(CMAKE_BUILD_TYPE "Release" CACHE STRING "Release or Debug")
#set(EigenVersion 2 CACHE STRING "Which eigen version to use: 2 or 3")
#set(EigenVectorize ON CACHE BOOL "Use eigen's auto-vectorization")
IF(NOT CMAKE_CONFIGURATION_TYPES AND NOT CMAKE_BUILD_TYPE)
   SET(CMAKE_BUILD_TYPE Release)
ENDIF(NOT CMAKE_CONFIGURATION_TYPES AND NOT CMAKE_BUILD_TYPE)


message("Using build type ${CMAKE_BUILD_TYPE}")

if(CMAKE_COMPILER_IS_GNUCXX)
	message("Compiler is Gcc")
	set(CMAKE_CXX_FLAGS_RELEASE "-DNDEBUG -m64 -O3 -march=native -mtune=native -fomit-frame-pointer")
  set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -ftree-loop-linear -ftree-loop-distribution -ftree-loop-im -ftree-loop-ivcanon -fivopts")#*slightly* good for build3v

  #set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -funroll-loops") #bad for build3,good for build3v
  #set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -ffast-math") #bad for build3,*slightly* good for build3v

	#set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -maccumulate-outgoing-args -msahf")#doesn't matter
  #set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -fgcse-sm -fgcse-las") #doesn't matter
  #set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -fvect-cost-model")#doesn't matter

  #set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -Wunsafe-loop-optimizations")
  #set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -funsafe-loop-optimizations")#bad for build3
	#set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -ftracer") #bad for build3
	set(CMAKE_CSS_FLAGS_DEBUG "-g3")
elseif(MSVC)
	message("Compiler is msvc")
	set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /EHa") #SEH exceptions
	set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /arch:SSE2") #SSE2 support
	set(CMAKE_CXX_FLAGS_RELEASE " /Ox /Ot /GL /fp:fast -DNDEBUG")
	set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /LTCG")
	set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /LTCG")
	set(CMAKE_MODULE_LINKER_FLAGS "${CMAKE_MODULE_LINKER_FLAGS} /LTCG")
	set(CMAKE_LIBRARY_LINKER_FLAGS "${CMAKE_LIBRARY_LINKER_FLAGS} /LTCG")
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