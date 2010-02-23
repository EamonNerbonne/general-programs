cmake_minimum_required(VERSION 2.8)
project(EigenBench)
message("Using build type ${CMAKE_BUILD_TYPE}")




if(CMAKE_COMPILER_IS_GNUCXX)
	message("Compiler is Gcc")
	set(CMAKE_CXX_FLAGS_RELEASE "-O3 -march=native -mtune=native -m64 -fomit-frame-pointer -DNDEBUG")
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

function(DoConfig EigenVersion Vectorize)
	
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

	set(exeName "BenchEigen${EigenVersion}")
	if(Vectorize)
		set(exeName "${exeName}v")
		remove_definitions(-DEIGEN_DONT_VECTORIZE)
	else()
		add_definitions(-DEIGEN_DONT_VECTORIZE)
	endif()
  
  set(sources DoTest.cpp learningBench.cpp  mulBench.cpp main.cpp)
  add_executable(${exeName}  ${sources})
endfunction()

DoConfig(3 ON)
DoConfig(2 ON)
DoConfig(3 OFF)
DoConfig(2 OFF)
