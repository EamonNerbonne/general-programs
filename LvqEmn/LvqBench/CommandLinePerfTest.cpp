// CommandLinePerfTest.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "BoostMatrixTest.h"

using namespace boost;
const int maxN = 10000000;
using namespace boost::numeric::ublas;

int _tmain(int argc, _TCHAR* argv[])
{
#if 0
	scoped_array<LogNumber> arr(new LogNumber[maxN]);
	for(int i=0;i<maxN;i++){
		arr[i] = LogNumber(double(i+1));
	}
	std::cout <<ln2<<std::endl;
	for(int i=0;i<1;i++){
		LogNumber sum(1.0);
		{progress_timer t;
		for(int j=0;j<maxN;j++) 
			sum=sum.AddSmallerSlow(arr[j]);
		for(int j=maxN-1;j>=0;j--) 
			sum=sum.AddSmallerSlow(arr[j]);
		}
		std::cout<<"Sum: "<<ToDouble(sum)<<std::endl;

		sum = LogNumber(1.0);
		{progress_timer t;
		for(int j=0;j<maxN;j++) 
			sum=sum.AddSmaller(arr[j]);
		for(int j=maxN-1;j>=0;j--) 
			sum=sum.AddSmaller(arr[j]);
		}
		std::cout<<"Sum: "<<ToDouble(sum)<<std::endl;

		sum = LogNumber(1.0);
		{progress_timer t;
		for(int j=0;j<maxN;j++) 
			sum=sum.AddSmaller1(arr[j]);
		for(int j=maxN-1;j>=0;j--) 
			sum=sum.AddSmaller1(arr[j]);
		}
		std::cout<<"Sum: "<<ToDouble(sum)<<std::endl;

		sum = LogNumber(1.0);
		{progress_timer t;
		for(int j=0;j<maxN;j++) 
			sum=sum.AddSmaller2(arr[j]);
		for(int j=maxN-1;j>=0;j--) 
			sum=sum.AddSmaller2(arr[j]);
		}
		std::cout<<"Sum: "<<ToDouble(sum)<<std::endl;

		sum = LogNumber(1.0);
		{progress_timer t;
		for(int j=0;j<maxN;j++) 
			sum=sum.AddSmaller3(arr[j]);
		for(int j=maxN-1;j>=0;j--) 
			sum=sum.AddSmaller3(arr[j]);
		}
		long double lsum = 1.0;
		{progress_timer t;
		for(int j=0;j<maxN;j++) 
			lsum+=j+1;
		for(int j=maxN-1;j>=0;j--) 
			lsum+=j+1;
		}
		std::cout<<"Sum: "<<ToDouble(lsum)<<std::endl;
		std::cout<<"sizeof(double): "<<sizeof(double)<<std::endl;
		std::cout<<"sizeof(long double): "<<sizeof(long double)<<std::endl;
	}


#else

#ifdef NDEBUG
	const int iters = 10000000;
#else
	const int iters = 50;
#endif
	const int dims = 4;
	for(int i=0;i<3;i++){
		{progress_timer t;
		BoostMatrixTest::TestMultCustom(iters,dims);}
		{progress_timer t;
		BoostMatrixTest::TestMultCustomColMajor(iters,dims);}
		{progress_timer t;
		BoostMatrixTest::TestMultRowMajor(iters,dims);}
		{progress_timer t;
		BoostMatrixTest::TestMultColMajor(iters,dims);}
		{progress_timer t;
		BoostMatrixTest::TestMultInline(iters,dims);}
		{progress_timer t;
		BoostMatrixTest::TestMultEigen(iters,dims);}
		//{progress_timer t;
		//BoostMatrixTest::TestMultEigenStatic<dims>(iters);}
#ifdef EIGEN_VECTORIZE
		std::cout << "eigen vectorized\n";
#endif 
#ifdef MTL_MTL_INCLUDE
		std::cout << "mtl\n";
		{progress_timer t;
		BoostMatrixTest::TestMultMtl (iters,dims);}
#endif
	}

#endif
	return 0;
}

