cmake_minimum_required(VERSION 2.8)
project (LvqLib)

if(NOT build_suffix)
   message(FATAL_ERROR "Build suffix not set!  Have you cmake-ed the outer directory?")
endif()

set(CMAKE_INCLUDE_CURRENT_DIR ON) 
set(sources LvqLib.cpp CreateDataset.cpp G2mLvqModel.cpp LgmLvqModel.cpp GmLvqModel.cpp GmFullLvqModel.cpp GgmLvqModel.cpp FgmLvqModel.cpp  GpqLvqModel.cpp LpqLvqModel.cpp GoodBadMatch.cpp LvqDataset.cpp LvqModel.cpp LvqModelSettings.cpp LvqProjectionModel.cpp NearestNeighbor.cpp NeuralGas.cpp PCA.cpp utils.cpp)

set(libName "LvqLib${build_suffix}")


IF (CMAKE_COMPILER_IS_GNUCXX)
    SET(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Wl,--output-def,${CMAKE_LIBRARY_OUTPUT_DIRECTORY}/${libName}.def")
ENDIF (CMAKE_COMPILER_IS_GNUCXX)

ADD_LIBRARY(${libName} SHARED ${sources})

IF (CMAKE_COMPILER_IS_GNUCXX)
	add_custom_command(TARGET ${libName} POST_BUILD COMMAND "C:/Program Files (x86)/Microsoft Visual Studio 11.0/VC/bin/amd64/lib.exe" ARGS /machine:x64 /def:${libName}.def WORKING_DIRECTORY ${CMAKE_LIBRARY_OUTPUT_DIRECTORY} VERBATIM )
ENDIF (CMAKE_COMPILER_IS_GNUCXX)
