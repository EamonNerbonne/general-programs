#pragma once
#ifdef _MSC_VER
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
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

#include <iostream>
#include <algorithm>
#include <numeric>
#include <vector>
#include <math.h>

#include <boost/smart_ptr/scoped_array.hpp>
//#include <boost/smart_ptr/shared_ptr.hpp>
#include <boost/smart_ptr/scoped_ptr.hpp>

#include <boost/function.hpp>
#include <boost/bind/bind.hpp>

#include <boost/random/variate_generator.hpp>
#include <boost/random/uniform_int.hpp>
#include <boost/random/normal_distribution.hpp>
#include <boost/random/mersenne_twister.hpp>

#ifdef _MSC_VER
#pragma warning(pop)
//#pragma warning(push,3)
#pragma warning(disable: 4714) //OK to ignore __forceinline
#endif
#include <Eigen/Core>
#include <Eigen/LU> 
#include <Eigen/QR> 
#include <Eigen/StdVector>
#include <Bench/BenchTimer.h>

using Eigen::MatrixBase;
using Eigen::VectorXd;
using Eigen::Vector2d;
using Eigen::VectorXi;
using Eigen::MatrixXd;
using Eigen::MatrixXi;


#ifdef _MSC_VER
//#pragma warning(pop)
#endif

#pragma warning (disable: 4127)
#define DBG(X) (std::cout<< #X <<":\n"<<(X)<<"\n")
