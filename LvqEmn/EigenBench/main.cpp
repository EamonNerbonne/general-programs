#include "EigenBench.h"


int main(int argc, char* argv[]){ 
    //mulBench(); 
#if EIGEN_DONT_VECTORIZE
	cout<< "EIGEN_DONT_VECTORIZE\n";
#endif
#if EIGEN3
	cout<< "EIGEN3\n";
#endif
#if EIGEN2
	cout<< "EIGEN2\n";
#endif
#ifdef NDEBUG
	cout<< "NDEBUG\n";
#endif
	learningBench();
    return 0; 
}
