#pragma once
#ifndef STANDALONE
#define BOOST_TEST_INCLUDED
#pragma warning (push)
#pragma warning (disable: 4702)
#include <boost/test/unit_test.hpp>
#pragma warning (pop)
#endif
#include "LvqTypedefs.h"
#include <Eigen/StdVector>
#include <iostream>
using namespace Eigen;
static bool failed = false;
#define ASSTRING(X) #X
#define VERIFY(X) do {bool ok=(X);failed |= !ok; std::cout<<ASSTRING(X)<<":\n"<<ok<<"\n"; } while(false)

using std::cout;
using std::cerr;

#pragma warning(disable: 4512)
#pragma warning(disable: 4714) //OK to ignore __forceinline

