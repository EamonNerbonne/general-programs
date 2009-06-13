// This is the main DLL file.

#include "stdafx.h"

#include "LVQCppCli.h"
#include "../LVQCppNative/BoostMatrixTest.h"

namespace LVQCppCli {

	void Class1::Testublas(int iters, int rows, int cols){
		BoostMatrixTest::TestMult(iters,rows);
	}

}