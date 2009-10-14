// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#define BOOST_UBLAS_NDEBUG

#pragma warning (disable:4996)
#pragma warning (disable:4099)

#include <iostream>

#pragma warning (push)
#pragma warning (disable:4522)
#pragma warning (disable:4355)
#pragma warning (disable:4267)
#include <boost/numeric/mtl/mtl.hpp>
#pragma warning (pop)


#include <boost/numeric/ublas/matrix.hpp>
#include <boost/smart_ptr/scoped_array.hpp>
#ifdef _MSC_VER
#include <C:\Program Files (Custom)\eigen2\Eigen\Core>
#else
#include <Eigen/Core>
#endif

// TODO: reference additional headers your program requires here
