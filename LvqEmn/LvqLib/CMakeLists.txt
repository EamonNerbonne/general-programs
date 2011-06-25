cmake_minimum_required(VERSION 2.8)
project (LvqLib)

if(NOT build_suffix)
   message(FATAL_ERROR "Build suffix not set!  Have you cmake-ed the outer directory?")
endif()

set(CMAKE_INCLUDE_CURRENT_DIR ON) 
set(sources LvqLib.cpp CreateDataset.cpp G2mLvqModel.cpp LgmLvqModel.cpp GmLvqModel.cpp GgmLvqModel.cpp GoodBadMatch.cpp LvqDataset.cpp LvqModel.cpp LvqModelSettings.cpp LvqProjectionModel.cpp NearestNeighbor.cpp NeuralGas.cpp PCA.cpp utils.cpp)

set(libName "LvqLib${build_suffix}")


IF (CMAKE_COMPILER_IS_GNUCXX)
    SET(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Wl,--output-def,${CMAKE_LIBRARY_OUTPUT_DIRECTORY}/${libName}.def")
    #GET_TARGET_PROPERTY(existing_link_flags ${libName} CMAKE_SHARED_LINKER_FLAGS)
    #IF(existing_link_flags)
    #    SET(new_link_flags "${existing_link_flags} ${new_link_flags}")
    #ENDIF()
    #SET_TARGET_PROPERTIES(${libName} PROPERTIES CMAKE_SHARED_LINKER_FLAGS ${new_link_flags})
    INSTALL(FILES ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}/lib${libName}.def DESTINATION lib)
ENDIF (CMAKE_COMPILER_IS_GNUCXX)

ADD_LIBRARY(${libName} SHARED ${sources})
