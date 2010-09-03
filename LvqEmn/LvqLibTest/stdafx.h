#pragma once

#ifndef STANDALONE
#define BOOST_TEST_INCLUDED
#include <boost/test/unit_test.hpp>
#endif
#include <Eigen/Core>
using namespace Eigen;
static bool failed = false;
#define ASSTRING(X) #X
#define VERIFY(X) do {bool ok=(X);failed |= !ok; std::cout<<ASSTRING(X)<<":\n"<<ok<<"\n"; } while(false)
