#pragma once
#include "stdafx.h"


template <typename T> T randomScalingMatrix(std::mt19937& rngParams, int dims, double detScalePower) {
    auto uniform01_rand = std::bind(std::uniform_real_distribution(0.0, 1.0), rngParams);
    T P = randomUnscalingMatrix<T>(rngParams, dims);
    P *= exp(uniform01_rand() * 2.0 * detScalePower - detScalePower);
    return P;
}
