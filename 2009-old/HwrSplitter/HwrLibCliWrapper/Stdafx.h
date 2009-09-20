// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once
#define _CRT_RAND_S

#if defined(_MANAGED)
#define NOMINMAX
#include <windows.h>
#undef FILETIME
#define BOOST_USE_WINDOWS_H
#endif



#include <boost/timer.hpp>
#include <msclr/auto_handle.h>
#include <iostream>

using namespace System;
using namespace System::Linq;
using namespace System::Runtime::InteropServices;
