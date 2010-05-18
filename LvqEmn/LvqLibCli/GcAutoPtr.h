//By Eamon Nerbonne 2010

//A managed container wrapping a read-only pointer to a native object
//nondetermistically destructed, but well suited for plain objects
#pragma once

template<typename T> ref class GcAutoPtr sealed {
	T*  ptr;
	size_t  size;
public:
	GcAutoPtr(T*ptr,size_t size) : ptr(ptr),size(size){
		GC::AddMemoryPressure(size);
	}

	!GcAutoPtr() {
		GC::RemoveMemoryPressure(size);
		size=0;
		delete ptr;
		ptr= nullptr;
	}

	~GcAutoPtr() { this->!GcAutoPtr();} //mostly just to avoid C4461

	T* get() {return ptr;}

	static T* operator->(GcAutoPtr<T>% gcPtr) { return gcPtr.ptr;}
	static operator T*(GcAutoPtr<T>% gcPtr) { return gcPtr.ptr; }
};

class GcPtr {
public:
	template<typename T>
	static GcAutoPtr<T>^ Create(T* ptr, size_t size) {return gcnew GcAutoPtr<T>(ptr,size);}
};