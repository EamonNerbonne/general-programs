#pragma once

#ifdef _MSC_VER
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#define _CRT_RAND_S
#pragma warning (disable:4996)
#pragma warning (disable:4099)
#endif

#include <bench/BenchTimer.h>
#define EIGEN_USE_NEW_STDVECTOR
#include <Eigen/StdVector>

#include <boost/timer.hpp>
#include <boost/progress.hpp>

#include <iostream>

#include <boost/numeric/ublas/matrix.hpp>
#include <boost/smart_ptr/scoped_array.hpp>
#include <boost/smart_ptr/scoped_ptr.hpp>
#include <boost/random/variate_generator.hpp>
#include <boost/random/uniform_int.hpp>
#include <boost/random/normal_distribution.hpp>
#include <boost/random/mersenne_twister.hpp>

#include <Eigen/Core>

