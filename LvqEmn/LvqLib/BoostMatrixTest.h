#pragma once
#include "stdafx.h"
#include <boost/numeric/ublas/matrix.hpp>

namespace BoostMatrixTest
{
	void TestMultCustom(int iters, int dims);
	void TestMultCustomColMajor(int iters, int dims);

    void TestMultRowMajor(int iters, int dims);
	void TestMultColMajor(int iters, int dims);

	void TestMultInline(int iters, int dims);
	void TestMultEigen(int iters, int dims);

	template <int dims> static void TestMultEigenStatic(int iters) {
		USING_PART_OF_NAMESPACE_EIGEN
		Matrix<double,dims,dims> a;
		Matrix<double,dims,dims> tmp;
		Matrix<double,dims,dims> b;
		a.setIdentity();
		tmp.setIdentity();
		b.setZero();
		for(int i=0;i<dims;i++){
			b(i,i)=1.000000001;
		}

		for(int i=0;i<iters;i++){
			a = b * a;
		}

		double matS=0;
		for(int i=0;i<dims*dims;i++)
			matS+=a.data()[i];
		std::cout << matS << "\nEigen<"<<typeid(a).name()<<"> prod took: ";
	}


#ifdef MTL_MTL_INCLUDE
	void TestMultMtl(int iters, int dims);
#endif
};
