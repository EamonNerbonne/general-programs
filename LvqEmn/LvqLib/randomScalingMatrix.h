#pragma once
#include "stdafx.h"


template <typename T> T randomScalingMatrix(std::mt19937 & rngParams, int dims,double detScalePower ) {
    boost::uniform_01<std::mt19937> uniform01_rand(rngParams);
    T P = randomUnscalingMatrix<T>(rngParams, dims);
    P*=exp(uniform01_rand()*2.0*detScalePower-detScalePower);
    return P;
}
