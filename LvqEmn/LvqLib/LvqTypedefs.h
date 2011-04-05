#pragma once
#include <Eigen/Core>
#define LVQ_LOW_DIM_SPACE 2

typedef double LvqFloat;
typedef double LvqStat;
typedef Eigen::Matrix<LvqFloat, LVQ_LOW_DIM_SPACE, Eigen::Dynamic> Matrix_2N;
typedef Matrix_2N Matrix_P;
typedef Eigen::Matrix<LvqFloat, Eigen::Dynamic, Eigen::Dynamic> Matrix_NN;
typedef Eigen::Matrix<LvqFloat, LVQ_LOW_DIM_SPACE, LVQ_LOW_DIM_SPACE> Matrix_22;
typedef Eigen::Matrix<LvqFloat, Eigen::Dynamic, 1> Vector_N;
typedef Eigen::Matrix<LvqFloat, LVQ_LOW_DIM_SPACE, 1> Vector_2;

typedef Eigen::Matrix<LvqStat, Eigen::Dynamic, 1> Vector_Stat;