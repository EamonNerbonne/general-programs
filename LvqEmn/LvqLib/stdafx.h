// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#ifndef _WIN32_WINNT            // Specifies that the minimum required platform is Windows Vista.
#define _WIN32_WINNT 0x0600     // Change this to the appropriate value to target other versions of Windows.
#endif

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#define BOOST_UBLAS_NDEBUG

#pragma warning (disable:4996)
#pragma warning (disable:4099)

#define EIGEN_USE_NEW_STDVECTOR
#include <Eigen/StdVector>
#include <iostream>
//#include <assert.h>
#include <algorithm>
#include <numeric>
#include <vector>
#include <math.h>


#include <boost/smart_ptr/scoped_array.hpp>
#include <boost/smart_ptr/shared_ptr.hpp>
#include <boost/smart_ptr/scoped_ptr.hpp>

#include <boost/function.hpp>
#include <boost/bind/bind.hpp>

#include <boost/random/variate_generator.hpp>
#include <boost/random/uniform_int.hpp>
#include <boost/random/uniform_real.hpp>
#include <boost/random/normal_distribution.hpp>
#include <boost/random/mersenne_twister.hpp>

//#define EIGEN_DONT_VECTORIZE
#include <Eigen/Core>
#define LVQ_LOW_DIM_SPACE 2

typedef Eigen::Matrix<double,LVQ_LOW_DIM_SPACE, Eigen::Dynamic> PMatrix;


