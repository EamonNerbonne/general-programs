#pragma once

using namespace System::IO;
using namespace System;

namespace EmnExtensionsNative {
	public  ref class StdoutRedirector
	{
		static Stream^ RedirectCStream(FILE* oldCstream, HANDLE* hCustomOutWr);
	public:
		static Stream^ RedirectStdout(void);
		static Stream^ RedirectStderr(void);
	};
}
