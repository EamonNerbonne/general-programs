#pragma once

#include <msclr/auto_handle.h>


namespace MyDisposables {
	using namespace System;
	using namespace msclr;

	public ref class MyDisposableContainer
	{
		auto_handle<IDisposable> kidObj;
		auto_handle<IDisposable> kidObj2;
	public:

		MyDisposableContainer(IDisposable^ a,IDisposable^ b) : kidObj(a), kidObj2(b)
		{
			Console::WriteLine("look ma, no destructor!");
		}
	};

	public
}