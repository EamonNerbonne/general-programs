cmake_minimum_required(VERSION 2.8)
project (LvqLib)

if(NOT build_suffix)
   message(FATAL_ERROR"Build suffix not set!  Have you cmake-ed the outer directory?")
endif()

set(CMAKE_INCLUDE_CURRENT_DIR ON) 
set(sources LvqModel.cpp DatasetUtils.cpp G2mLvqModel.cpp GmLvqModel.cpp GsmLvqModel.cpp LvqDataset.cpp utils.cpp)

set(libName "LvqLib${build_suffix}")
ADD_LIBRARY(${libName} STATIC ${sources})

