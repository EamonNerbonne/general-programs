#pragma once


//represents a value that's stored by pointer: only useful to affect memory locality; semantically just a value
template <typename T> class copy_ptr {
    T* item;
public:
    explicit copy_ptr(T const& existingItem) : item(new T(existingItem)) {}
    copy_ptr(copy_ptr<T> const& other) : item(new T(*other.item)) {}
    copy_ptr(copy_ptr<T>&& tmpother) : item(tmpother.item) { tmpother.item = 0; }

    ~copy_ptr() { delete item; }

    T const* get() const { return item; }
    T const* operator->() const { return get(); }
    T const& operator*() const { return *item; }
    T* get() { return item; }
    T* operator->() { return get(); }
    T& operator*() { return *item; }

    copy_ptr& operator=(copy_ptr const& rhs) { *item = *rhs.item; return *this; }
    copy_ptr& operator=(copy_ptr&& tmprhs) { item = tmprhs.item; tmprhs.item = 0; return *this; }
};
