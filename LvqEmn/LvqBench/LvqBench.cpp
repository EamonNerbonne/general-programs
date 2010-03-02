#include "stdafx.h"
#include "BoostMatrixTest.h"
#include "EasyLvqTest.h"

using namespace boost;
const int maxN = 10000000;
using namespace boost::numeric::ublas;


int main(int argc, char* argv[]){ 
	using std::cout;
	cout<<"LvqBench";
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

	progress_timer t(std::cerr);
	EasyLvqTest();
	std::cerr<<"Total time:";
	return 0;
}

void MatrixSpeedTest() {
	const int iters = 10000000;
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
}

