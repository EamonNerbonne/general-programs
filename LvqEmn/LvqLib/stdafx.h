#pragma once
#ifdef _MSC_VER
#define WIN32_LEAN_AND_MEAN // Exclude rarely-used stuff from Windows headers
#pragma warning (disable:4996)
#pragma warning (disable:4099)

#pragma warning(disable: 4505)
#pragma warning(disable: 4512)

#pragma warning(push,3)
#endif

#define EIGEN_VECTORIZE_SSE3
#define EIGEN_VECTORIZE_SSSE3
//#define EIGEN_VECTORIZE_SSE4_1
#define EIGEN_USE_NEW_STDVECTOR
#define _USE_MATH_DEFINES


#ifdef _MSC_VER
#pragma warning(pop)
//#pragma warning(push,3)
#pragma warning(disable: 4714) //OK to ignore __forceinline
#pragma warning(disable: 4244) //temporarily OK to ignore 64 vs. 32 bit issues. //TODO: remove 
#endif
#include <Eigen/Core>
#include <Eigen/LU> 
#include <Eigen/QR> 
#include <Eigen/StdVector>
//EIGEN_DEFINE_STL_VECTOR_SPECIALIZATION(Eigen::Matrix2d)

//EIGEN_DEFINE_STL_VECTOR_SPECIALIZATION(Eigen::Vector2d)

#include <Bench/BenchTimer.h>


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


#ifdef _MSC_VER
//#pragma warning(pop)
#pragma warning (disable: 4127)
#endif

#define DBG(X) (std::cout<< #X <<":\n"<<(X)<<"\n")
