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
		/*for(int row=0;row< a.size1();row++) {
			for(int col=0;col<a.size2();col++) {
				double val = a(row,col);
				std::cout<< a(row,col) <<" ";
			}
			std::cout<<std::endl;
		}*/
	}
	void Class1::TestublasNative(int iters, int dims){
		BoostMatrixTest::TestMult(iters,dims);
	}

}