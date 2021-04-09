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
  //Matrix<Scalar,Size,Dynamic,StorageOrder> tmp = data.colwise() - data.col(0);
  cov.noalias() = (data.colwise() - data.col(0)) * (data.colwise() - data.col(0)).adjoint();
}

template<typename Scalar,int Size, int StorageOrder>
EIGEN_DONT_INLINE void cov2(const Matrix<Scalar,Size,Dynamic,StorageOrder>& data, Matrix<Scalar,Size,Size,StorageOrder>& cov)
{
  cov.setZero();
  Matrix<Scalar,Size,Dynamic,StorageOrder> tmp = data.colwise() - data.col(0);
  cov.template selfadjointView<Lower>().rankUpdate(tmp);
}

template<typename Scalar,int Size, int StorageOrder>
EIGEN_DONT_INLINE void cov3(const Matrix<Scalar,Size,Dynamic,StorageOrder>& data, Matrix<Scalar,Size,Size,StorageOrder>& cov)
{
  cov.setZero();
  Matrix<Scalar,Size,1> tmp;
  for(int i=0; i<data.cols(); ++i)
  {
    tmp = data.col(i) - data.col(0);
    cov.noalias() += tmp * tmp.adjoint();
  }
}

template<typename Scalar,int Size, int StorageOrder>
EIGEN_DONT_INLINE void cov4(const Matrix<Scalar,Size,Dynamic,StorageOrder>& data, Matrix<Scalar,Size,Size,StorageOrder>& cov)
{
  cov.setZero();
  Matrix<Scalar,Size,1> tmp;
  for(int i=0; i<data.cols(); ++i)
  {
    tmp = data.col(i) - data.col(0);
    cov.template selfadjointView<Lower>().rankUpdate(tmp);
  }
}

template<typename Scalar,int Size, int StorageOrder>
EIGEN_DONT_INLINE void cov5(const Matrix<Scalar,Size,Dynamic,StorageOrder>& data, Matrix<Scalar,Size,Size,StorageOrder>& cov)
{
  cov.setZero();
  Matrix<Scalar,Size,1> tmp;
  for(int i=0; i<data.cols(); ++i)
  {
    tmp = data.col(i) - data.col(0);
    cov.template triangularView<Lower>() += tmp * tmp.adjoint();
  }
}

template<typename Scalar,int Size, int StorageOrder>
EIGEN_DONT_INLINE void cov6(const Matrix<Scalar,Size,Dynamic,StorageOrder>& data, Matrix<Scalar,Size,Size,StorageOrder>& cov)
{
  cov.setZero();
  Matrix<Scalar,Size,1> tmp;
  for(int i=0; i<data.cols(); ++i)
  {
    tmp = data.col(i) - data.col(0);
    for(int j=0;j<cov.cols();++j)
        for(int k=j;k<cov.cols();++k) 
            cov.coeffRef(k,j) += tmp.coeff(j) *tmp.coeff(k);
  }
}

#ifdef _MSC_VER
#pragma warning (disable: 4127)
#endif
#define BENCHOUT(tries,repeats,code) \
    do { \
        BenchTimer BENCHOUT_timer; \
        BENCH(BENCHOUT_timer, tries, repeats, code); \
        std::cout<<BENCHOUT_timer.best()<<"s, "; \
        break; \
    } while(true)

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

  std::cout << typeid(Scalar).name() << " " << Size << " " << size << " " << n << " => ";

  BENCHOUT(tries, repeats, (cov1< Scalar,Size,StorageOrder>(data,cov)));
  BENCHOUT(tries, repeats, (cov2< Scalar,Size,StorageOrder>(data,cov)));
  BENCHOUT(tries, repeats, (cov3< Scalar,Size,StorageOrder>(data,cov)));
  BENCHOUT(tries, repeats, (cov4< Scalar,Size,StorageOrder>(data,cov)));
  BENCHOUT(tries, repeats, (cov5< Scalar,Size,StorageOrder>(data,cov)));
  BENCHOUT(tries, repeats, (cov6< Scalar,Size,StorageOrder>(data,cov)));
  
  std::cout<<"\n";
}

typedef double scalarType;
int docovbench()
{
  int n = 2000;
  benchcov<scalarType,2,ColMajor>(n);
  benchcov<scalarType,3,ColMajor>(n);
  benchcov<scalarType,4,ColMajor>(n);
  benchcov<scalarType,Dynamic,ColMajor>(n,16);
  benchcov<scalarType,Dynamic,ColMajor>(n,128);
  //n = 200;
  //benchcov<scalarType,2,ColMajor>(n);
  //benchcov<scalarType,3,ColMajor>(n);
  //benchcov<scalarType,4,ColMajor>(n);
  //benchcov<scalarType,Dynamic,ColMajor>(n,16);
  //benchcov<scalarType,Dynamic,ColMajor>(n,128);
  //n = 20;
  //benchcov<scalarType,2,ColMajor>(n);
  //benchcov<scalarType,3,ColMajor>(n);
  //benchcov<scalarType,4,ColMajor>(n);
  //benchcov<scalarType,Dynamic,ColMajor>(n,16);
  //benchcov<scalarType,Dynamic,ColMajor>(n,128);
  return 0;
}
