#include "StdAfx.h"
#include "BoostMatrixTest.h"

BoostMatrixTest::BoostMatrixTest(void)
{
}

BoostMatrixTest::~BoostMatrixTest(void)
{
}

void BoostMatrixTest::TestMult(int iters, int rows) {
		using namespace boost::numeric::ublas;

		matrix<double> a = identity_matrix<double>(rows);
		matrix<double> b = identity_matrix<double>(rows);
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