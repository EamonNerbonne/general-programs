#pragma once

//#pragma managed(push, off)

#include <Eigen/Core>
#include <Eigen/QR> 
#include <Eigen/SVD> 
#include <boost/random/variate_generator.hpp>
#include <boost/random/mersenne_twister.hpp>
#include <boost/random/uniform_int.hpp>
#include <boost/random/normal_distribution.hpp>
#include <boost/function.hpp>
#include <boost/bind/bind.hpp>
#include <math.h>

#include "LvqTypedefs.h"
#define DBG(X) (std::cout<< #X <<":\n"<<(X)<<"\n")
#ifdef NDEBUG
#define DEBUGPRINT(X) ((void)0)
#else
#define DEBUGPRINT(X) DBG(X)
#endif

//randomizes all values of the matrix; each is independently drawn from a normal distribution with provided mean and sigma (=stddev).
template<typename T> void RandomMatrixInit(boost::mt19937 & rng, Eigen::MatrixBase< T>& mat, double mean, double sigma) {
	using namespace boost;
	normal_distribution<> distrib(mean,sigma);
	variate_generator<mt19937&, normal_distribution<> > rndGen(rng, distrib);
	for(int j=0; j<mat.cols(); j++)
		for(int i=0; i<mat.rows(); i++)
			mat(i,j) = rndGen();
}


template <typename T> T & as_lvalue(T && temporary_value) {return temporary_value;}

template <typename T> T sqr(T val) {return val*val;}



template <typename T> EIGEN_STRONG_INLINE double projectionSquareNorm(T const & projectionMatrix) {
	return (projectionMatrix.transpose() * projectionMatrix).diagonal().sum();
}

 template <typename T> void normalizeProjection(T & projectionMatrix) {
	projectionMatrix *= LvqFloat(LvqFloat(1.0)/sqrt(projectionSquareNorm(projectionMatrix)));
}


inline bool almostEqual(double x, double y, double leeway=1.0) {
	double diff = fabs(x-y);
	double sum = fabs(x+y);
	return diff <= leeway*std::numeric_limits<double>::epsilon()*sum;
}

template <typename T> void projectionRandomizeUniformScaled(boost::mt19937 & randGen, T & projectionMatrix) { //initializes all coefficients randomly to -1..1, then normalizes.
	boost::uniform_01<boost::mt19937> uniform01_rand(randGen);

	for(int col=0; col < projectionMatrix.cols(); col++)
		for(int row=0; row < projectionMatrix.rows(); row++) //column-major storage
			projectionMatrix(row,col) = uniform01_rand()*2.0-1.0;
		
	normalizeProjection(projectionMatrix);
}

void makeRandomOrder(boost::mt19937 & randGen, int*const toFill, int count);
Matrix_NN shuffleMatrixCols(boost::mt19937 & randGen, Matrix_NN const & src);

#ifdef _MSC_VER
#define FOREACH(RANGEVAR_DECL, ITERATOR) for each(RANGEVAR_DECL in ITERATOR)
#else
#define FOREACH(RANGEVAR_DECL, ITERATOR) for(RANGEVAR_DECL : ITERATOR)
#endif

//#pragma managed(pop)
#ifdef _MSC_VER
#define isfinite_emn(x)     (_finite(x)) 
#else
inline bool isfinite_emn(double x) {return !(std::isinf(x)  || std::isnan(x));}
#endif
