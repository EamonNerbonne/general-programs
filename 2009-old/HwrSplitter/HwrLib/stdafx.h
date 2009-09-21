// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once


// The following macros define the minimum required platform.  The minimum required platform
// is the earliest version of Windows, Internet Explorer etc. that has the necessary features to run 
// your application.  The macros work by enabling all features available on platform versions up to and 
// including the version specified.

// Modify the following defines if you have to target a platform prior to the ones specified below.
// Refer to MSDN for the latest info on corresponding values for different platforms.
#ifndef _WIN32_WINNT            // Specifies that the minimum required platform is Windows Vista.
#define _WIN32_WINNT 0x0600     // Change this to the appropriate value to target other versions of Windows.
#endif

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers

#define _USE_MATH_DEFINES
#define _CRT_RAND_S

#if defined(_MANAGED)
#define NOMINMAX
#include <windows.h>
#define BOOST_USE_WINDOWS_H
#endif

#ifdef _MSC_VER
#pragma warning(disable:4996) // deprecated
#endif

#include <stdlib.h>
#include <iostream>
#include <iomanip>
#include <algorithm>
#include <limits>
#include <math.h>
#include <assert.h>
#include <stdio.h>
#include <limits.h>
#include <math.h>
#include <string>
#include <fstream>
#include <vector>
#include <map>
#include <set>
#include <boost/scoped_ptr.hpp>
#include <wchar.h>
#include <sstream>
#include <stack>
#include <queue>
#include <boost/random/uniform_real.hpp>
#include <boost/random/variate_generator.hpp>
#include <boost/random/mersenne_twister.hpp>




