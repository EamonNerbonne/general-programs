template <typename T> class copy_val {
    T  item;
public:

    explicit copy_val(T const& existingItem) : item(existingItem) {}
    copy_val(copy_val<T> const& other) : item(other.item) {}
    copy_val(copy_val<T>&& tmpother) : item(std::move(tmpother.item)) {}
    ~copy_val() { }

    T const* get() const { return &item; }
    T const* operator->() const { return get(); }
    T const& operator*() const { return item; }
    T* get() { return &item; }
    T* operator->() { return get(); }
    T& operator*() { return item; }

    copy_val& operator=(copy_val const& rhs) { item = rhs.item; return *this; }
    copy_val& operator=(copy_val&& tmprhs) { item = std::move(tmprhs.item); return *this; }
};

