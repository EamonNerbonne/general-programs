#pragma once
#pragma warning(disable:4505)

#include <iostream>
#include <Bench/BenchTimer.h>
#include <Eigen/Core>
#if !EIGEN3
#include <Eigen/Array>
#endif

using namespace Eigen;
using namespace std;

#define DIMS 25
#define BENCH_RUNS 5


