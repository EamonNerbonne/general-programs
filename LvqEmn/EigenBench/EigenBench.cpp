#include "standard.h"

#include "diagUpdateBench.h"

#include "projectionBench.h"
#include "subtractBench.h"
#include "matmulTest.h"

int main(int , char* []){ 
	cout<<"EigenBench";
#if EIGEN3
	cout<< "3";
#else
#if EIGEN2
	cout<< "2";
#else
	cout<<"????";
#endif
#endif
#ifndef EIGEN_DONT_VECTORIZE
	cout<< "v";
#endif
#ifndef NDEBUG
	cout<< "[DEBUG]";
#endif
#ifdef _MSC_VER
	cout << " on MSC";
#else
#ifdef __GNUC__
	cout << " on GCC";
#else
	cout << " on ???";
#endif
#endif
	cout<<": ";

	//projectionTest();
	//subtractTest();
	cout << matmulTest() <<"s\n";
	return 0; 
}
