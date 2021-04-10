#pragma once

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable: 4793) // I don't care that you do SSE stuff in native mode, not managed mode... that's kinda the point, actually!
#pragma warning(disable: 4714) //OK to ignore __forceinline
#pragma warning(disable: 4510) //OK to not create default constructor
#pragma warning(disable: 4610) //OK to not create default constructor
#pragma warning(disable: 4701)
#endif

#include <Eigen/Core>
#ifdef _MSC_VER
#pragma warning(pop)
#endif

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

typedef Eigen::Matrix<unsigned char, Eigen::Dynamic, Eigen::Dynamic, Eigen::RowMajor> ClassDiagramT;