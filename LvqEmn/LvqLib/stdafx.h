#pragma once

//#define EIGEN_DONT_VECTORIZE
//#define EIGEN_VECTORIZE_SSE3
//#define EIGEN_VECTORIZE_SSSE3
//#define EIGEN_VECTORIZE_SSE4_1
//#define EIGEN_VECTORIZE_SSE4_2
//#define EIGEN_VECTORIZE_AVX
//#define EIGEN_VECTORIZE_FMA

#ifdef NDEBUG
#define EIGEN_NO_AUTOMATIC_RESIZING
#endif
//#define EIGEN_USE_NEW_STDVECTOR
#define _USE_MATH_DEFINES


#ifdef _MSC_VER
#define WIN32_LEAN_AND_MEAN // Exclude rarely-used stuff from Windows headers
#pragma warning(disable: 4714) //OK to ignore __forceinline
#pragma warning(disable: 4127) //"conditional expression is constant" - tons of those in eigen
#pragma warning(push)
#pragma warning(disable: 4996) // Don't warn about standard functions that are "unsafe" because they don't validate input.
#pragma warning(disable: 4510) //OK to not create default constructor
#pragma warning(disable: 4610) //OK to not create default constructor
#pragma warning(disable: 4701) //too many false positives for uninitalized locals
#pragma warning(disable: 4800)//eigen internal perf warning
#pragma warning(disable: 4702) //unreachable code in eigen?  probably some kind of template specialization noise
#else
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wmaybe-uninitialized"
#endif

#include <Eigen/Core>
#include <Eigen/LU> 
#include <Eigen/QR> 
#include <Eigen/StdVector>

#ifdef _MSC_VER
#pragma warning(disable: 4100) //unreferenced formal parameter
#pragma warning(disable: 4505) //unreferenced local function has been removed
#else
#pragma GCC diagnostic ignored "-Wunused-function"
#endif

#include <bench/BenchTimer.h>

#ifdef _MSC_VER
#pragma warning(pop)
#else
#pragma GCC diagnostic pop
#endif


#include <cmath>
#include <iostream>
#include <algorithm>
#include <random>
#include <numeric>
#include <vector>
#include <memory>


using Eigen::MatrixBase;

using Eigen::VectorXi;
using Eigen::MatrixXi;



