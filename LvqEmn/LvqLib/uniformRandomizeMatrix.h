#pragma once
#include <random>
#include <Eigen/Core>
#include "randomMatrixInit.h"



template <typename T> void uniformRandomizeMatrix(T& mat, std::mt19937& rngParams, double min, double max) {
    auto rndGen = std::bind(std::uniform_real_distribution(min, max), rngParams);
    for (int j = 0; j < mat.cols(); j++)
        for (int i = 0; i < mat.rows(); i++)
            mat(i, j) = rndGen();
}


