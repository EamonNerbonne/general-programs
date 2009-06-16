// This is the main DLL file.

#include "stdafx.h"

#include "LVQCppCli.h"
#include "../LVQCppNative/BoostMatrixTest.h"

namespace LVQCppCli {

	void Class1::Testublas(int iters, int dims){
		using namespace boost::numeric::ublas;

		matrix<double> a = identity_matrix<double>(dims);
		matrix<double> b = identity_matrix<double>(dims); 
		for(int i=0;i<iters;i++)
			a=prod(b,a);
	}
	void Class1::TestublasNative(int iters, int dims){BoostMatrixTest::TestMult<row_major>(iters,dims);}
	void Class1::TestCustomNative(int iters, int dims){BoostMatrixTest::TestMultCustom(iters,dims);}

}