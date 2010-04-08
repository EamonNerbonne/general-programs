#include <stdio.h>
template <typename T> struct Params;

template<typename TDerived>
struct GeneralBase  {
	enum{ IsVector = Params<TDerived>::IsVector  };
	typedef typename Params<TDerived>::ValueType ValueType;
};


template<typename TDerived>
struct ContainerBase : GeneralBase<TDerived> {
	typedef GeneralBase<TDerived> Base;
	using Base::IsVector; //causes ICE!
	//doesn't cause ICE:
	//enum{ IsVector = Params<TDerived>::IsVector  };
	
	typedef typename Params<TDerived>::ValueType ValueType;
};

template <typename T, int dims>
struct Container : ContainerBase<Container<T, dims> > {
	int getSize() {return dims*sizeof(T);}
};

template <typename T, int dims>
struct Params<Container<T, dims> > {
	typedef T ValueType;
	enum{ IsVector = (dims == 1) };
};

template<typename TDerived>
Container<typename ContainerBase<TDerived>::ValueType, /*required for ICE:*/ (ContainerBase<TDerived>::IsVector?1:2)> ConvertX(ContainerBase<TDerived> const & x){ 
	Container<typename ContainerBase<TDerived>::ValueType, (ContainerBase<TDerived>::IsVector?1:2)> nx;
	return nx;
}

int main()
{
	ContainerBase<Container<int,2>> v2i;
	printf("%d\n", ConvertX(v2i).getSize() ); //8

    return 0;
}
