#pragma once


#define NOMINMAX
#define WIN32_LEAN_AND_MEAN


#include <msclr/auto_handle.h>
#include <msclr/lock.h>  
#include <iostream>

#pragma warning(disable: 4127) //"conditional expression is constant" - tons of those in eigen
#pragma warning(disable: 4714) //OK to ignore __forceinline
#pragma warning(push)
#pragma warning(disable: 4793) // I don't care that you do SSE stuff in native mode, not managed mode... that's kinda the point, actually!
#pragma warning(disable: 4714) //OK to ignore __forceinline
#pragma warning(disable: 4510) //OK to not create default constructor
#pragma warning(disable: 4610) //OK to not create default constructor
#pragma warning(disable: 4701)
#pragma managed(push, off)
#include <Eigen/Core>
#pragma managed(pop)
#pragma warning(pop)

#include "GcAutoPtr.h"
#include "WrappingUtils.h"
