#include "EigenBench.h"


int main(int argc, char* argv[]){ 
    //mulBench(); 
#if EIGEN_DONT_VECTORIZE
	cout<< "EIGEN_DONT_VECTORIZE\n";
#endif
	learningBench();
    return 0; 
}
