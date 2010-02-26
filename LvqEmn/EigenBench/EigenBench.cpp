#include "standard.h"

#include "diagUpdateBench.h"

#include "projectionBench.h"
#include "subtractBench.h"


int main(int argc, char* argv[]){ 
	cerr<<"EigenBench";
#if EIGEN3
	cerr<< "3";
#else
#if EIGEN2
	cerr<< "2";
#else
	cerr<<"????";
#endif
#endif
#ifndef EIGEN_DONT_VECTORIZE
	cerr<< "v";
#endif
#ifndef NDEBUG
	cerr<< "[DEBUG]";
#endif
	cerr<<": ";

	//projectionTest();
	subtractTest();
	return 0; 
}
