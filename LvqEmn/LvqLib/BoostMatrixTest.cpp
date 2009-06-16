#include "StdAfx.h"
#include "BoostMatrixTest.h"



void BoostMatrixTest::TestMultCustom(int iters, int dims) {
	using namespace std;
	using namespace boost;
	
	scoped_array<double> a (new double[dims*dims]);
	scoped_array<double> tmp(new double[dims*dims]);
	scoped_array<double> b (new double[dims*dims]);
	for(int i=0;i<dims*dims;i++)
		a[i]=b[i]=0.0;


	for(int i=0;i<dims;i++){
		a[i*dims+i]=1;
		b[i*dims+i]=1.000000001;
	}

	for(int i=0;i<iters;i++){
		for(int row=0;row<dims;row++) { // A <- B*A
			double* bRow = b.get()+row*dims;
			for(int col=0;col<dims;col++) {
				double* aCol = a.get()+col;

				double sum=0.0;
				for(int k=0;k<dims;k++) {
					sum+=aCol[k*dims]*bRow[k];
				}
				tmp[col+row*dims]=sum;
			}
		}
		a.swap(tmp);
		//swap(a,tmp);
		//a=prod(b,a);
	}

	double matS=0;
	for(int i=0;i<dims*dims;i++)
		matS+=a[i];
	printf("%f\nArray based mult took: ",matS);
}

void BoostMatrixTest::TestMultCustomColMajor(int iters, int dims) {
	using namespace std;
	using namespace boost;
	scoped_array<double> a (new double[dims*dims]);//colmajor
	scoped_array<double> b (new double[dims*dims]);//rowmajor
	scoped_array<double> tmp( new double[dims*dims]);//colmajor

	for(int i=0;i<dims*dims;i++)
		a[i]=b[i]=0.0;
	for(int i=0;i<dims;i++){
		a[i*dims+i]=1;
		b[i*dims+i]=1.000000001;
	}

	for(int i=0;i<iters;i++){
		double *tmpCurr=tmp.get();
		double* aCol = a.get();
		for(int col=0;col<dims;col++) {
			for(int row=0;row<dims;row++) { // A <- B*A
				double* bRow = b.get()+row*dims;

				double sum=0.0;
				for(int k=0;k<dims;k++) 
					sum+=aCol[k]*bRow[k];
				
				*(tmpCurr++)=sum;
			}
			aCol+=dims;
		}
		a.swap(tmp);
		//swap(a,tmp);
		//a=prod(b,a);
	}

	double matS=0;
	for(int i=0;i<dims*dims;i++)
		matS+=a[i];
	printf("%f\nArray based mult (col-major) took: ",matS);
}