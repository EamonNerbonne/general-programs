#include <stdio.h>
template <typename T> struct Params;

template<typename TDerived>
struct GeneralBase  {
	enum{ ParamVal = Params<TDerived>::ParamVal  };
	typedef TDerived Derived;
};

template <int val>
struct Container : GeneralBase<Container<val > > {
	typedef GeneralBase<Container<val > > Base;
	int getVal() {return val;}

	using Base::ParamVal; //triggers bug
	//enum{ ParamVal = Params<typename Base::Derived>::ParamVal  };	//doesn't trigger bug
};

template <int val>
struct Params<Container< val> > {
	enum{ ParamVal = val };
};

#define CAUSE_CRASH (Container<val>::ParamVal?0:1)
#define CAUSE_C2135 (!Container<val>::ParamVal)
#define CAUSE_C2597 (Container<val>::ParamVal + 1)

#define TEMPLATE_PARAM CAUSE_CRASH

template<int val>
Container<TEMPLATE_PARAM> ConvertX(Container<val> const & x){ 
	return Container<TEMPLATE_PARAM>();
}

int main() {
	Container<0> v2i;
	printf("%d\n", ConvertX(v2i).getVal() ); //should print 1
    return 0;
}
