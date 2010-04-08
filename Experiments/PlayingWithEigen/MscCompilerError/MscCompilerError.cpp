#include <stdio.h>
template <typename T> struct Params;

template<typename TDerived>
struct GeneralBase  {
	enum{ IsVector = Params<TDerived>::IsVector  };
};


template<typename TDerived>
struct ContainerBase : GeneralBase<TDerived> {
	typedef GeneralBase<TDerived> Base;
	using Base::IsVector; //causes ICE!
	//doesn't cause ICE:
	//enum{ IsVector = Params<TDerived>::IsVector  };
	
};

template < int dims>
struct Container : ContainerBase<Container< dims> > {
	int getSize() {return dims;}
};

template < int dims>
struct Params<Container< dims> > {
	enum{ IsVector = (dims == 1) };
};

template<typename TDerived>
Container</*required for ICE:*/ (ContainerBase<TDerived>::IsVector?1:2)> ConvertX(ContainerBase<TDerived> const & x){ 
	Container<(ContainerBase<TDerived>::IsVector?1:2)> nx;
	return nx;
}

int main()
{
	ContainerBase<Container<2>> v2i;
	printf("%d\n", ConvertX(v2i).getSize() ); //8

    return 0;
}
