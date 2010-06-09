// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once
//#pragma warning (disable:4996) //yes, the managed-mode reference assemblies may at compile time be for a different platform.  Since they're managed, however, this will work runtime.
#pragma warning (disable:4793) // I don't care that you do SSE stuff in native mode, not managed mode... that's kinda the point, actually!

#include <msclr/auto_handle.h>
#include <msclr/lock.h>  
#include <iostream>
#include "GcAutoPtr.h"
#include "AbstractLvqModel.h"
#include "WrappingUtils.h"