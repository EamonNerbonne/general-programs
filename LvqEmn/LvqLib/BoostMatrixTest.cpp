#include "StdAfx.h"
#include "BoostMatrixTest.h"

BoostMatrixTest::BoostMatrixTest(void)
{
}

BoostMatrixTest::~BoostMatrixTest(void)
{
}

void BoostMatrixTest::TestMult(int iters, int dims) {
		using namespace boost::numeric::ublas;

		matrix<double,column_major> a = identity_matrix<double,column_major>(dims);
		matrix<double,column_major> b = identity_matrix<double,column_major>(dims); 
		//matrix<double> c = identity_matrix<double>(dims); 
		for(int i=0;i<iters;i++){
			a=prod(b,a);
			//a=c;
		}
		/*for(int row=0;row< a.size1();row++) {
			for(int col=0;col<a.size2();col++) {
				double val = a(row,col);
				std::cout<< a(row,col) <<" ";
			}
			std::cout<<std::endl;
		}*/


}