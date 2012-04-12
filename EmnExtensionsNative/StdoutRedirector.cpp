#include "StdoutRedirector.h"

#define UNICODE 1
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include <io.h>
#include <fcntl.h>
#include <ios>
#include <msclr/auto_handle.h>
#include "GcAutoPtr.h"
using namespace System::Threading;
using namespace Microsoft::Win32::SafeHandles;
using namespace System;
using namespace std;
namespace EmnExtensionsNative {
	RestoringReadStream::RestoringReadStream(FileStream^readStream, FILE origFileValue, FILE* underlyingOutStream, FILE*newOutStream) 
		: readStream(readStream)
		, origFileValue(new FILE(origFileValue))
		, underlyingOutStream(underlyingOutStream) 
		, newOutStream(newOutStream)
	{
	}

	RestoringReadStream::!RestoringReadStream() { 
		fclose(newOutStream); 
		*underlyingOutStream = *origFileValue; 
		delete origFileValue; 
		origFileValue=nullptr; 
		underlyingOutStream = nullptr; 
		newOutStream = nullptr;
	}

	RestoringReadStream::~RestoringReadStream() { 
		this->!RestoringReadStream(); 
	} 

	FileStream^ RestoringReadStream::ReadStream::get(){return readStream.get();} 
	//the core idea here is taken from various sources, of which http://www.halcyon.com/~ast/dload/guicon.htm is the most complete (although it attempts a slightly different use-case)


		//SetStdHandle( STD_OUTPUT_HANDLE,hCustomStdoutWr);//doesn't seem necessary.
		//SetStdHandle(STD_ERROR_HANDLE,hCustomStderrWr);//doesn't seem necessary.

	//ios::sync_with_stdio();//in theory this should remake cout...//doesn't seem necessary.


	RestoringReadStream^ StdoutRedirector::RedirectCStream(FILE* nativeOutStream){
		HANDLE hCustomOutRd,hCustomOutWr;
		SECURITY_ATTRIBUTES *pipeSecurity=NULL;//SECURITY_ATTRIBUTES are null since we don't plan on ever exposing this internal pipe
		int bufSize=0;//buffer size, where 0 means auto-selected. 
		if (!CreatePipe(&hCustomOutRd, &hCustomOutWr, pipeSecurity, bufSize)) //CloseHandle() must be called on hCustomOutRd and hCustomOutWr eventually.
			throw gcnew Exception(gcnew String(L"Could not create pipe for native stream redirection!"));

		int outHandle = _open_osfhandle((intptr_t)hCustomOutWr, _O_APPEND); //insert magic here(1) - this converts a win32 handle to... a file descriptor (i.e., yet another handle)!
		//calling _close() on outHandle will close hCustomOutWr
		
		FILE* newOut=_fdopen(outHandle,"w"); //insert magic here(2) - this converts a file descriptor to a FILE* (i.e., yet another handle)!
		//calling fclose() on newOut will close outHandle, closing hCustomOutWr
		
		setvbuf(newOut,NULL,_IONBF,0);//we don't want buffering.
		FILE oldValue = *nativeOutStream;
		*nativeOutStream = *newOut;

		//we use a safehandle since firstly this takes care of deallocation, and since FileStream requires it.
		//this will only close the hCustomOutRd! hCustomOutWr also needs closing.
		SafeFileHandle^ safeCustomOutRd = gcnew SafeFileHandle((IntPtr)hCustomOutRd,true); //Disposing safeCustomOutRd will close hCustomOutRd
		
		FileStream^ cliReadStream = gcnew FileStream(safeCustomOutRd,FileAccess::Read);		//Disposing cliReadStream will Dispose safeCustomOutRd will close hCustomOutRd
		return gcnew RestoringReadStream(cliReadStream,oldValue,nativeOutStream,newOut);
	}
}