//By Eamon Nerbonne 2010

//A managed container wrapping a read-only pointer to a native object
//nondetermistically destructed, but well suited for plain objects
#pragma once

template<typename T> ref class GcAutoPtr sealed {
    T* ptr;
    size_t  size;
public:
    GcAutoPtr(T* ptr, size_t size) : ptr(ptr), size(size) { GC::AddMemoryPressure(size); }

    !GcAutoPtr() {
        GC::RemoveMemoryPressure(size);
        size = 0;
        delete ptr;
        ptr = nullptr;
    }

    ~GcAutoPtr() { this->!GcAutoPtr(); }

    T* get() { return ptr; }

    static T* operator->(GcAutoPtr<T>% gcPtr) { return gcPtr.ptr; }
    static operator T* (GcAutoPtr<T>% gcPtr) { return gcPtr.ptr; }
};

template<typename T> ref class GcPlainPtr sealed {
    T* ptr;
public:
    GcPlainPtr(T* ptr) : ptr(ptr) { GC::AddMemoryPressure(sizeof(T)); }

    !GcPlainPtr() { GC::RemoveMemoryPressure(sizeof(T)); delete ptr; ptr = nullptr; }

    ~GcPlainPtr() { this->!GcPlainPtr(); }

    T* get() { return ptr; }

    static T* operator->(GcPlainPtr<T>% gcPtr) { return gcPtr.ptr; }
    static operator T* (GcPlainPtr<T>% gcPtr) { return gcPtr.ptr; }
};

class GcPtr {
public:
    template<typename T>
    static GcAutoPtr<T>^ Create(T* ptr, size_t size) { return gcnew GcAutoPtr<T>(ptr, size); }

    template<typename T>
    static GcAutoPtr<T>^ Create(T* ptr) { return gcnew GcAutoPtr<T>(ptr, ptr->MemAllocEstimate()); }
};