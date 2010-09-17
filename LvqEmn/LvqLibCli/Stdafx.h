#pragma once
//#pragma warning (disable:4996) //yes, the managed-mode reference assemblies may at compile time be for a different platform.  Since they're managed, however, this will work runtime.
#pragma warning (disable:4793) // I don't care that you do SSE stuff in native mode, not managed mode... that's kinda the point, actually!

#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#pragma warning(disable: 4714) //OK to ignore __forceinline

#include <msclr/auto_handle.h>
#include <msclr/lock.h>  
#include <iostream>
#include <Eigen/Core>
#include "GcAutoPtr.h"
#include "WrappingUtils.h"
