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
	std::cout<<matS<<"\nArray based mult took: ";
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
	
	std::cout<<matS<<"\nArray based mult (col-major) took: ";
}

void BoostMatrixTest::TestMultInline(int iters, int dims) {
	using namespace std;
	using namespace boost::numeric::ublas;

	matrix<double,column_major> a = identity_matrix<double,column_major>(dims);
	matrix<double,column_major> tmp = identity_matrix<double,column_major>(dims);
	matrix<double,row_major> b = identity_matrix<double,row_major>(dims); 
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
	std::cout<<matS<<"\nBoost<"<<typeid(a).name()<<"> prod took: ";
}

void BoostMatrixTest::TestMultEigen(int iters, int dims) {
	using namespace boost;
	USING_PART_OF_NAMESPACE_EIGEN
		//		using namespace std;
		//	using namespace boost::numeric::ublas;
	scoped_ptr<MatrixXd> a(new MatrixXd(dims,dims));
	scoped_ptr<MatrixXd> tmp(new MatrixXd(dims,dims));
	scoped_ptr<MatrixXd> b(new MatrixXd(dims,dims));
	a->setIdentity();
	tmp->setIdentity();
	b->setZero();
	for(int i=0;i<dims;i++){
		(*b)(i,i)=1.000000001;
	}

	for(int i=0;i<iters;i++){
#if EIGEN3
		(*tmp).noalias() = *b * *a;
#else
		*tmp = (*b * *a).lazy();
#endif
		swap(a, tmp);
	}

	double matS=0;
	for(int i=0;i<dims*dims;i++)
		matS+=a->data()[i];
	std::cout<<matS<<"\nEigen<"<<typeid(a).name()<<"> prod took: ";
}

template <class row_or_col_major> void BoostTestMult(int iters, int dims) {
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
	std::cout<<matS<<"\nBoost<" << typeid(row_or_col_major).name()<<"> prod took: ";
}

void BoostMatrixTest::TestMultRowMajor(int iters, int dims) { 
	BoostTestMult<boost::numeric::ublas::row_major>(iters,dims);
}
void BoostMatrixTest::TestMultColMajor(int iters, int dims){ 
	BoostTestMult<boost::numeric::ublas::column_major>(iters,dims);
}



#ifdef MTL_MTL_INCLUDE
void BoostMatrixTest::TestMultMtl(int iters, int dims) {
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
	std::cout <<matS<<"\nMTL prod took: ";
}
#endif
