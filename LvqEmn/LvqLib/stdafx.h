#pragma once
#ifdef _MSC_VER
#endif



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
#define EIGEN_USE_NEW_STDVECTOR
#define _USE_MATH_DEFINES


#ifdef _MSC_VER
#define WIN32_LEAN_AND_MEAN // Exclude rarely-used stuff from Windows headers
#pragma warning(disable: 4714) //OK to ignore __forceinline
#pragma warning(push)
#pragma warning(disable: 4996) // Don't warn about standard functions that are "unsafe" because they don't validate input.
#pragma warning(disable: 4510) //OK to not create default constructor
#pragma warning(disable: 4610) //OK to not create default constructor
#pragma warning(disable: 4701) //too many false positives for uninitalized locals
#pragma warning(disable: 4800)//eigen internal perf warning
#endif

#include <Eigen/Core>
#include <Eigen/LU> 
#include <Eigen/QR> 
#include <Eigen/StdVector>
#include <Bench/BenchTimer.h>

#ifdef _MSC_VER
#pragma warning(pop)
#endif


#include <cmath>

#include <iostream>
#include <algorithm>
#include <numeric>
#include <vector>


#include <boost/smart_ptr/scoped_array.hpp>
#include <boost/smart_ptr/scoped_ptr.hpp>

#include <boost/function.hpp>
#include <boost/bind/bind.hpp>

#include <boost/random/variate_generator.hpp>
#include <boost/random/uniform_int.hpp>
#include <boost/random/normal_distribution.hpp>
#include <boost/random/mersenne_twister.hpp>

using Eigen::MatrixBase;

using Eigen::VectorXi;
using Eigen::MatrixXi;



