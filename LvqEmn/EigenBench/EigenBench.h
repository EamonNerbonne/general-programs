#pragma once
//#define EIGEN_DONT_VECTORIZE 1
#include <boost/progress.hpp>
#include <Eigen/Core>
#if !EIGEN3
#include <Eigen/Array>
#endif

using namespace Eigen;
using namespace boost;
using std::cout;
using std::endl;

void mulBench(void);
void learningBench(void);