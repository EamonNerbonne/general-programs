// CommandLinePerfTest.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "BoostMatrixTest.h"
#include "EasyLvqTest.h"

using namespace boost;
const int maxN = 10000000;
using namespace boost::numeric::ublas;

int _tmain(int argc, _TCHAR* argv[])
{
	progress_timer t;
	EasyLvqTest();
	//EigenBench();
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

