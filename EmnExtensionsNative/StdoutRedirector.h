#pragma once

using namespace System::IO;
using namespace System;
#include <msclr/auto_handle.h>
#include "GcAutoPtr.h"

#define UNICODE 1
#define WIN32_LEAN_AND_MEAN
#include <stdio.h>

namespace EmnExtensionsNative {
	public ref class RestoringReadStream {
		msclr::auto_handle<FileStream> readStream;
		FILE* origFileValue;
		FILE* underlyingOutStream;
		FILE* newOutStream;
	public:
		RestoringReadStream(FileStream^readStream, FILE origFileValue, FILE* underlyingOutStream, FILE*newOutStream); 
		!RestoringReadStream();
		~RestoringReadStream();
		property FileStream^ ReadStream {  FileStream^ get(); }
	};

	public  ref class StdoutRedirector
	{
		static RestoringReadStream^ RedirectCStream(FILE* nativeOutStream);
	public:
		static RestoringReadStream^ RedirectStdout(void){		return RedirectCStream(stdout);	}

		static RestoringReadStream^ RedirectStderr(void){		return RedirectCStream(stderr);	}
	};
}
