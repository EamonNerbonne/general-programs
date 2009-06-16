// CommandLinePerfTest.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

using namespace boost;

int _tmain(int argc, _TCHAR* argv[])
{
#ifdef NDEBUG
	const int iters = 500000;
#else
	const int iters = 50;
#endif
	const int dims = 8;
	for(int i=0;i<3;i++){
		{progress_timer t;
		BoostMatrixTest::TestMultCustom(iters,dims);}
		{progress_timer t;
		BoostMatrixTest::TestMultCustomColMajor(iters,dims);}
		{progress_timer t;
		BoostMatrixTest::TestMult<row_major>(iters,dims);}
		{progress_timer t;
		BoostMatrixTest::TestMult<column_major>(iters,dims);}
#ifdef MTL_MTL_INCLUDE
		{progress_timer t;
		BoostMatrixTest::TestMultMtl (iters,dims);}
#endif
	}

	return 0;
}

