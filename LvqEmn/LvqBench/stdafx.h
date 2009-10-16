// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers

#ifndef _WIN32_WINNT            // Specifies that the minimum required platform is Windows Vista.
#define _WIN32_WINNT 0x0600     // Change this to the appropriate value to target other versions of Windows.
#endif

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
#ifdef _MSC_VER
#include <C:\Program Files (Custom)\eigen2\Eigen\Core>
#else
#include <Eigen/Core>
#endif

