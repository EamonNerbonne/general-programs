// CommandLinePerfTest.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"


int _tmain(int argc, _TCHAR* argv[])
{
				const int iters = 5000000;
			const int dims = 3;

	BoostMatrixTest::TestMult(iters,dims);

	BoostMatrixTest::TestMultCustom(iters,dims);
	return 0;
}

