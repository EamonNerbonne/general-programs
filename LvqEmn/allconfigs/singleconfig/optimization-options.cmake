IF(NOT CMAKE_CONFIGURATION_TYPES AND NOT CMAKE_BUILD_TYPE)
   SET(CMAKE_BUILD_TYPE Release)
ENDIF(NOT CMAKE_CONFIGURATION_TYPES AND NOT CMAKE_BUILD_TYPE)

message("Using build type ${CMAKE_BUILD_TYPE}")
if(CMAKE_COMPILER_IS_GNUCXX)
	message("Compiler is Gcc")
  
#  if($ENV{NUMBER_OF_PROCESSORS} EQUAL "8")
#    set(CPU_ARCH "corei7")
#    message("Using i7 without AVX")
#  else ()
    set(CPU_ARCH "native")
#  endif()
	set(CMAKE_CXX_FLAGS_DEBUG " -Wfatal-errors -Wall -Wno-unused-local-typedefs -Wno-parentheses -std=c++11 -m64 -march=${CPU_ARCH} -mtune=native -O2")
	set(CMAKE_CXX_FLAGS_RELEASE "  -Wfatal-errors -Wall -Wno-unused-local-typedefs -Wno-parentheses -std=c++11  -m64 -march=${CPU_ARCH} -mtune=native -O3 ")
	
    set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -DNDEBUG")

  set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -ftree-loop-distribution -ftree-loop-im -ftree-loop-ivcanon -fivopts")#*slightly* good for build3v
  set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -fassociative-math -fno-trapping-math -fno-signed-zeros -fno-math-errno")#-ffinite-math-only  -funsafe-math-optimizations -ffast-math
  #fast-math can cause crashes.
  
  set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -funroll-loops") #bad for build3,good for build3v

  set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -maccumulate-outgoing-args -msahf")#doesn't matter
  set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -fgcse-sm -fgcse-las") #doesn't matter
  set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -fvect-cost-model")#doesn't matter

  #set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -ftree-loop-linear -floop-interchange -floop-strip-mine -floop-block")
  #set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -Wsuggest-attribute=const")
  #set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -flto ")#-fwhole-program

#-ftree-parallelize-loops=4 -floop-interchange 
  #set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -Wunsafe-loop-optimizations")
    set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -funsafe-loop-optimizations ")#bad for build3
	set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -ftracer") #bad for build3

	set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -s")
	set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -s ")
	set(CMAKE_MODULE_LINKER_FLAGS "${CMAKE_MODULE_LINKER_FLAGS} -s ")
	set(CMAKE_LIBRARY_LINKER_FLAGS "${CMAKE_LIBRARY_LINKER_FLAGS} -s ")
elseif(MSVC)
	message("Compiler is msvc")
	set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /EHa /arch:SSE2 /W4") #SEH exceptions,SSE2 support, extra warnings
	set(CMAKE_CXX_FLAGS_RELEASE " /Ox /Ot /GL /Oy /Oi /GS- /fp:fast /fp:except- /MD -DNDEBUG")
	set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /LTCG /OPT:REF /OPT:ICF /MACHINE:X64 /INCREMENTAL:NO")
	set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /LTCG /OPT:REF /OPT:ICF /MACHINE:X64  /INCREMENTAL:NO")
	set(CMAKE_MODULE_LINKER_FLAGS "${CMAKE_MODULE_LINKER_FLAGS}  /LTCG /OPT:REF /OPT:ICF /MACHINE:X64  /INCREMENTAL:NO")
	set(CMAKE_LIBRARY_LINKER_FLAGS "${CMAKE_LIBRARY_LINKER_FLAGS}  /LTCG /OPT:REF /OPT:ICF /MACHINE:X64  /INCREMENTAL:NO")
else()
	message(FATAL_ERROR "Unrecognized compiler!")
endif()
