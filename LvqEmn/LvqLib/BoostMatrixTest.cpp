#include "StdAfx.h"
#include "BoostMatrixTest.h"

BoostMatrixTest::BoostMatrixTest(void){}

BoostMatrixTest::~BoostMatrixTest(void){}

void BoostMatrixTest::TestMult(int iters, int dims) {
	using namespace std;

	using namespace boost::numeric::ublas;

	matrix<double,column_major> a = identity_matrix<double,column_major>(dims);
	matrix<double> b = identity_matrix<double>(dims); 
	for(int i=0;i<dims;i++){
		a(i,i)=1;
		b(i,i)=1.000000001;
	}

	for(int i=0;i<iters;i++){
		a=prod(b,a);
	}
	/*for(int row=0;row< a.size1();row++) {
	for(int col=0;col<a.size2();col++) {
	double val = a(row,col);
	std::cout<< a(row,col) <<" ";
	}
	std::cout<<std::endl;
	}*/
	double matS=0;
	for(int i=0;i<dims*dims;i++)
		matS+=a.data()[i];
	printf("%f\n",matS);
	//	cout<<matS<<endl;

	//return 
}

void BoostMatrixTest::TestMultCustom(int iters, int dims) {
	using namespace std;
	using namespace boost;
	scoped_array<double> a (new double[dims*dims]);//colmajor
	scoped_array<double> b (new double[dims*dims]);//rowmajor
	scoped_array<double> tmp( new double[dims*dims]);//colmajor
	for(int i=0;i<dims;i++){
		a[i*dims+i]=1;
		b[i*dims+i]=1.000000001;
	}

	for(int i=0;i<iters;i++){
		for(int row=0;row<dims;row++) { // A <- B*A
			double* bRow = b.get()+row*dims;

			for(int col=0;col<dims;col++) {
				double* aCol = a.get()+col*dims;
				double sum=0.0;
				for(int k=0;k<dims;k++) {
					sum+=aCol[k]*bRow[k];
				}
				tmp[col*dims+row]=sum;

			}
		}
		a.swap(tmp);
		//a=prod(b,a);
	}

	double matS=0;
	for(int i=0;i<dims*dims;i++)
		matS+=a[i];
	printf("%f\n",matS);
}