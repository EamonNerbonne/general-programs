#include "stdafx.h"
#include "StdoutRedirector.h"
using namespace System::Threading;
using namespace Microsoft::Win32::SafeHandles;
using namespace System;
using namespace std;
namespace EmnExtensionsNative {
	//the core idea here is taken from various sources, of which http://www.halcyon.com/~ast/dload/guicon.htm is the most complete (although it attempts a slightly different use-case)

	//TODO: stick in a separate assembly and make a EmnExtensionsWindows using _that_ for so for all this native stuff.

	Stream^ StdoutRedirector::RedirectStdout(void){
		HANDLE hCustomStdoutWr;
		Stream^ managedStream = RedirectCStream(stdout, &hCustomStdoutWr);

		SetStdHandle(STD_OUTPUT_HANDLE,hCustomStdoutWr);//not sure if this is necessary...
		ios::sync_with_stdio();//in theory this should remake cout...
		return managedStream;
	}

	Stream^ StdoutRedirector::RedirectStderr(void){
		HANDLE hCustomStderrWr;
		Stream^ managedStream = RedirectCStream(stderr, &hCustomStderrWr);

		SetStdHandle(STD_ERROR_HANDLE,hCustomStderrWr);//not sure if this is necessary...
		ios::sync_with_stdio();//in theory this should remake cout...
		return managedStream;
	}


	Stream^ StdoutRedirector::RedirectCStream(FILE* oldCstream, HANDLE* phCustomOutWr){

		HANDLE hCustomOutRd;	
		//SECURITY_ATTRIBUTES are null since we don't plan on ever exposing this internal pipe
		SECURITY_ATTRIBUTES *pipeSecurity=NULL;
		int bufSize=0;//buffer size, where 0 means auto-selected. 
		if (! CreatePipe(&hCustomOutRd, phCustomOutWr, pipeSecurity, bufSize)) 
			throw gcnew Exception(gcnew String(L"Could not create pipe for stdout!"));



		//insert magic here - this converts a win32 handle to some other handle:
		int outHandle = _open_osfhandle((intptr_t)*phCustomOutWr,_O_APPEND);
		//insert magic here - this converts some other handle to a FILE*
		FILE* newOut=_fdopen(outHandle,"w");
		setvbuf(newOut,NULL,_IONBF,0);//we don't want buffering.

		*oldCstream = *newOut;

		//we use a safehandle since firstly this takes care of deallocation, and since FileStream requires it.
		SafeFileHandle^ safeCustomOutRd = gcnew SafeFileHandle((IntPtr)hCustomOutRd,true);
		return gcnew FileStream(safeCustomOutRd,FileAccess::Read);		
		//TODO: uhh, cleanup, deallocation of rest of pipe? huh? walks away...
	}
}