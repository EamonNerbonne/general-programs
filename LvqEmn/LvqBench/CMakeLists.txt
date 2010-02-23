cmake_minimum_required(VERSION 2.8)
project (LibLvq)

if(NOT build_suffix)
   message(FATAL_ERROR"Build suffix not set!  Have you cmake-ed the outer directory?")
endif()

set(LibLvq_VERSION_MAJOR 0)
set(LibLvq_VERSION_MINOR 5)

set(CMAKE_INCLUDE_CURRENT_DIR ON) 


set(sources BoostMatrixTest.cpp DataSetUtils.cpp G2mLvqModel.cpp GmLvqModel.cpp GsmLvqModel.cpp LvqDataSet.cpp stdafx.cpp utils.cpp)

set(libName "LibLvq${build_suffix}")
ADD_LIBRARY(${libName} STATIC ${sources})

