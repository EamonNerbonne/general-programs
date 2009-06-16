#pragma once
#include "stdafx.h"
using namespace boost::numeric::ublas;

//template <class row_or_col_major = row_major>class BoostMatrixTest;

class BoostMatrixTest
{
public:
	//BoostMatrixTest(void);
	//~BoostMatrixTest(void);
	template <class row_or_col_major> static void TestMult(int iters, int dims) {
		using namespace std;
		using namespace boost::numeric::ublas;

		matrix<double,row_or_col_major> a = identity_matrix<double,row_or_col_major>(dims);
		matrix<double,row_or_col_major> tmp = identity_matrix<double,row_or_col_major>(dims);
		matrix<double,row_or_col_major> b = identity_matrix<double,row_or_col_major>(dims); 
		for(int i=0;i<dims;i++){
			a(i,i)=1;
			b(i,i)=1.000000001;
		}

		for(int i=0;i<iters;i++){
			noalias(tmp)=prod(b,a);
			swap(a,tmp);
		}
		double matS=0;
		for(int i=0;i<dims*dims;i++)
			matS+=a.data()[i];
		printf("%f\nBoost<%s> prod took: ",matS,typeid(row_or_col_major).name());
		//	cout<<matS<<endl;

		//return 

	}
	static void TestMultCustom(int iters, int dims);
	static void TestMultCustomColMajor(int iters, int dims);

#ifdef MTL_MTL_INCLUDE
	static void TestMultMtl(int iters, int dims) {
		using namespace std;
		using namespace mtl;

		dense2D<double> a(dims,dims); 
		dense2D<double> tmp(dims,dims);
		dense2D<double> b(dims,dims);
		a=1;
		b=1.000000001;

		for(int i=0;i<iters;i++){
			tmp=b*a;
			swap(a,tmp);
		}
		double matS=0;
		for(int i=0;i<dims;i++)
			for(int j=0;j<dims;j++)
				matS+=a(i,j);
		printf("%f\nMTL prod took: ",matS);
	}
#endif
};
