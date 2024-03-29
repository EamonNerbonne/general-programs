#pragma once

#ifdef _MSC_VER
#define WIN32_LEAN_AND_MEAN // Exclude rarely-used stuff from Windows headers
#define _CRT_RAND_S
#pragma warning (disable:4996)
#pragma warning (disable:4714)
#pragma warning (push, 3)
#pragma warning (disable:4099)
#pragma warning (disable:4701)
#pragma warning (disable:4505)
#else
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wunused-function"
#endif

//#define EIGEN_DONT_VECTORIZE
//#define EIGEN_VECTORIZE_SSE3
//#define EIGEN_VECTORIZE_SSSE3
//#define EIGEN_VECTORIZE_SSE4_1
#define EIGEN_USE_NEW_STDVECTOR

#include <bench/BenchTimer.h>
#include <Eigen/StdVector>
#include <iostream>
#include <random>

#ifdef _MSC_VER
#pragma warning (push)
#pragma warning (disable:4996)
#pragma warning (disable:4714)
#pragma warning (disable:4099)
#pragma warning (disable:4701)
#pragma warning (disable:4505)
#endif

#include <Eigen/Core>

#ifdef _MSC_VER
#pragma warning(pop)
#else
#pragma GCC diagnostic pop
#endif

