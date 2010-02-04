// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers

#ifndef _WIN32_WINNT            // Specifies that the minimum required platform is Windows Vista.
#define _WIN32_WINNT 0x0600     // Change this to the appropriate value to target other versions of Windows.
#endif
#define _CRT_RAND_S

#include <stdio.h>
#include <tchar.h>

#include <boost/timer.hpp>
#include <boost/progress.hpp>

#pragma warning (disable:4996)
#pragma warning (disable:4099)

#include <iostream>

#include <boost/numeric/ublas/matrix.hpp>
#include <boost/smart_ptr/scoped_array.hpp>
#include <boost/smart_ptr/scoped_ptr.hpp>
#include <boost/random/variate_generator.hpp>
#include <boost/random/uniform_int.hpp>
#include <boost/random/normal_distribution.hpp>
#include <boost/random/mersenne_twister.hpp>
#include <boost/nondet_random.hpp>

//#define EIGEN_DONT_VECTORIZE


#include <Eigen/Core>
#include <Eigen/Array>
//#include <Bench/BenchTimer.h>
