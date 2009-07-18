// FloatBench.cpp : Defines the entry point for the console application.
//


#include <stdio.h>

#include <boost/timer.hpp>
#include <boost/progress.hpp>
#include <boost/scoped_array.hpp>
using namespace boost;

const int NUMEL = 100;
const int NUMITER = 100000000;
typedef float FloatT;

int main(int argc, char* argv[])
{
	scoped_array<FloatT> arr1(new FloatT[NUMEL]);
	scoped_array<FloatT> arr2(new FloatT[NUMEL]);
	scoped_array<FloatT> arr3(new FloatT[NUMEL]);
	scoped_array<FloatT> arr4(new FloatT[NUMEL]);
	for(int i=0;i<NUMEL;i++) {
		arr1[i]=FloatT(1.0) - FloatT(0.000001)*i/FloatT(3.0);
		arr2[i]=FloatT(1.0) + FloatT(0.000001)*i/FloatT(5.0);
		arr3[i]=FloatT(1.0) + FloatT(0.000001)*i/FloatT(7.0);
		arr4[i]=FloatT(1.0) - FloatT(0.000001)*i/FloatT(11.0);
	}
	FloatT bla(1.0);
	FloatT someval(0.99999999999);

	{
		progress_timer t;
		for(int j=0;j<NUMITER;j++) {
			for(int i=0;i<NUMEL;i++) {
			    bla += arr1[i] * arr2[i];// + arr3[i] * arr4[i] ;
			}
		}
	}

	std::cout<< bla<<std::endl;

	return 0;
}

