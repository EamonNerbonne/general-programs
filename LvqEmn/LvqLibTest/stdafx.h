#pragma once
#ifndef STANDALONE
#define BOOST_TEST_INCLUDED
#pragma warning (push)
#pragma warning (disable: 4702)
#include <boost/test/unit_test.hpp>
#pragma warning (pop)
#endif

#pragma warning(disable: 4127) //"conditional expression is constant" - tons of those in eigen
#pragma warning(disable: 4512)
#pragma warning(disable: 4714) //OK to ignore __forceinline
#pragma warning(disable: 4702) //unreachable code - makes sense due to eigen

#pragma warning (push)
#pragma warning(disable: 4701) //potentially uninitialized local variable 'i' used
#pragma warning(disable: 4100) //unreferenced formal parameter
#pragma warning(disable: 4505) //unreferenced local function has been removed
#include <bench/BenchTimer.h>
#include <Eigen/Core>
#include <Eigen/StdVector>
#pragma warning (pop)

#include <random>
#include <iostream>
#include "LvqTypedefs.h"

using namespace Eigen;
static bool failed = false;
#define ASSTRING(X) #X
#define VERIFY(X) do {bool ok=(X);failed |= !ok; std::cout<<ASSTRING(X)<<":\n"<<ok<<"\n"; } while(false)

using std::cout;
using std::cerr;

