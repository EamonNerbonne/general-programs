//By Eamon Nerbonne 2010

//A managed container wrapping a read-only pointer to a native object
//nondetermistically destructed, but well suited for plain objects
#pragma once

template<typename T> ref class GcPtr sealed {
	T*  ptr;
	size_t  size;
public:
	GcPtr(T*ptr,size_t size) : ptr(ptr),size(size){
		GC::AddMemoryPressure(size);
	}

	!GcPtr() {
		GC::RemoveMemoryPressure(size);
		size=0;
		delete ptr;
		ptr= nullptr;
	}

	~GcPtr() { this->!GcPtr();} //mostly just to avoid C4461

	T* get() {return ptr;}

	static T* operator->(GcPtr<T>% gcPtr) { return gcPtr.ptr;}
	static operator T*(GcPtr<T>% gcPtr) { return gcPtr.ptr; }
};

class GcPtrHelp {
public:
	template<typename T>
	static GcPtr<T>^ Create(T* ptr, size_t size) {return gcnew GcPtr<T>(ptr,size);}
};