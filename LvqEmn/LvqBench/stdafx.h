#pragma once

#ifdef _MSC_VER
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#define _CRT_RAND_S
#pragma warning (disable:4996)
#pragma warning (disable:4099)
#pragma warning (disable:4505)
#pragma warning (push, 3)
#endif

#define EIGEN_VECTORIZE_SSE3
#define EIGEN_VECTORIZE_SSSE3
#define EIGEN_VECTORIZE_SSE4_1
#define EIGEN_USE_NEW_STDVECTOR

#include <bench/BenchTimer.h>
#include <Eigen/StdVector>

#include <iostream>

#include <boost/smart_ptr/scoped_ptr.hpp>
#include <boost/random/variate_generator.hpp>
#include <boost/random/uniform_int.hpp>
#include <boost/random/normal_distribution.hpp>
#include <boost/random/mersenne_twister.hpp>


#ifdef _MSC_VER
#pragma warning(pop)
#endif
#include <Eigen/Core>

