#include <stdio.h>
template <typename T> struct Params;

template<typename TDerived>
struct GeneralBase  {
	enum{ IsVector = Params<TDerived>::IsVector  };
};


template <int val>
struct Container : GeneralBase<Container<val > > {
	typedef GeneralBase<Container<val > > Base;
	using Base::IsVector; //causes ICE!
	//doesn't cause ICE:
	//enum{ IsVector = Params<Base>::IsVector  };
	int getSize() {return val;}
};

template <typename TDerived>
struct Params<GeneralBase<TDerived> > {
	enum{ IsVector = Params<TDerived>::IsVector };
};

template <int val>
struct Params<Container< val> > {
	enum{ IsVector = (val == 1) };
};

template<int val>
Container</*required for ICE:*/ (Container<val>::IsVector?1:2)> ConvertX(Container<val> const & x){ 
	Container<(Container<val>::IsVector?1:2)> nx;
	return nx;
}

int main()
{
	Container<2> v2i;
	printf("%d\n", ConvertX(v2i).getSize() ); //8

    return 0;
}
