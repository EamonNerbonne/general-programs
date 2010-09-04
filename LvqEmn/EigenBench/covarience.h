#include <bench/BenchTimer.h>
#include <iostream>
#include <typeinfo>
#include <Eigen/Eigen>
using namespace Eigen;

// #ifndef SCALAR
// #define SCALAR float
// #endif

// typedef SCALAR Scalar;

// static const int Size;
// typedef Matrix<Scalar,Size,Dynamic,RowMajor> Data;

template<typename Scalar,int Size, int StorageOrder>
EIGEN_DONT_INLINE void cov1(const Matrix<Scalar,Size,Dynamic,StorageOrder>& data, Matrix<Scalar,Size,Size,StorageOrder>& cov)
{
  Matrix<Scalar,Size,Dynamic,StorageOrder> tmp = data.colwise() - data.col(0);
  cov.noalias() = tmp * tmp.adjoint();
}

template<typename Scalar,int Size, int StorageOrder>
EIGEN_DONT_INLINE void cov2(const Matrix<Scalar,Size,Dynamic,StorageOrder>& data, Matrix<Scalar,Size,Size,StorageOrder>& cov)
{
  cov.setZero();
  Matrix<Scalar,Size,Dynamic,StorageOrder> tmp = data.colwise() - data.col(0);
  cov.template selfadjointView<Lower>().rankUpdate(data);
}

template<typename Scalar,int Size, int StorageOrder>
EIGEN_DONT_INLINE void cov3(const Matrix<Scalar,Size,Dynamic,StorageOrder>& data, Matrix<Scalar,Size,Size,StorageOrder>& cov)
{
  cov.setZero();
  for(int i=0; i<data.cols(); ++i)
  {
    Matrix<Scalar,Size,1> tmp = data.col(i) - data.col(0);
    cov.noalias() += tmp * tmp.adjoint();
  }
}

template<typename Scalar,int Size, int StorageOrder>
EIGEN_DONT_INLINE void cov4(const Matrix<Scalar,Size,Dynamic,StorageOrder>& data, Matrix<Scalar,Size,Size,StorageOrder>& cov)
{
  cov.setZero();
  for(int i=0; i<data.cols(); ++i)
  {
    Matrix<Scalar,Size,1> tmp = data.col(i) - data.col(0);
    cov.template selfadjointView<Lower>().rankUpdate(tmp);
  }
}

template<typename Scalar,int Size, int StorageOrder>
EIGEN_DONT_INLINE void cov5(const Matrix<Scalar,Size,Dynamic,StorageOrder>& data, Matrix<Scalar,Size,Size,StorageOrder>& cov)
{
  cov.setZero();
  for(int i=0; i<data.cols(); ++i)
  {
    Matrix<Scalar,Size,1> tmp = data.col(i) - data.col(0);
    cov.template triangularView<Lower>() += tmp * tmp.adjoint();
  }
}



template<typename Scalar,int Size, int StorageOrder>
void benchcov(int n, int size = Size)
{
  Matrix<Scalar,Size,Dynamic,StorageOrder> data(size,n);
  data.setRandom();
  Matrix<Scalar,Size,Size,StorageOrder> cov(size,size);
  cov.setZero();

  int tries = 4;
  int repeats = int(10000000/(double(n)*size*size));
  repeats = repeats==0 ? 1 : repeats;

  BenchTimer t1, t2, t3, t4, t5;
  BENCH(t1, tries, repeats, (cov1< Scalar,Size,StorageOrder>(data,cov)));
  BENCH(t2, tries, repeats, (cov2< Scalar,Size,StorageOrder>(data,cov)));
  BENCH(t3, tries, repeats, (cov3< Scalar,Size,StorageOrder>(data,cov)));
  BENCH(t4, tries, repeats, (cov4< Scalar,Size,StorageOrder>(data,cov)));
  BENCH(t5, tries, repeats, (cov5< Scalar,Size,StorageOrder>(data,cov)));
  
  std::cout << typeid(Scalar).name() << " " << Size << " " << size << " " << n
            << " => " << t1.best()
            << "s , " << t2.best()
            << "s , " << t3.best()
            << "s , " << t4.best()
            << "s , " << t5.best()
            << "s\n";
}

typedef double scalarType;
int docovbench()
{
  int n = 20;
  benchcov<scalarType,2,ColMajor>(n);
  benchcov<scalarType,3,ColMajor>(n);
  benchcov<scalarType,4,ColMajor>(n);
  benchcov<scalarType,Dynamic,ColMajor>(n,16);
  benchcov<scalarType,Dynamic,ColMajor>(n,128);
  n = 200;
  benchcov<scalarType,2,ColMajor>(n);
  benchcov<scalarType,3,ColMajor>(n);
  benchcov<scalarType,4,ColMajor>(n);
  benchcov<scalarType,Dynamic,ColMajor>(n,16);
  benchcov<scalarType,Dynamic,ColMajor>(n,128);
  n = 2000;
  benchcov<scalarType,2,ColMajor>(n);
  benchcov<scalarType,3,ColMajor>(n);
  benchcov<scalarType,4,ColMajor>(n);
  benchcov<scalarType,Dynamic,ColMajor>(n,16);
  benchcov<scalarType,Dynamic,ColMajor>(n,128);
  return 0;
}
