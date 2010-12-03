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


template <typename T> T & as_lvalue(T && temporary_value) {return temporary_value;}

template <typename T> T sqr(T val) {return val*val;}

void makeRandomOrder(boost::mt19937 & randGen, int*const toFill, int count);

template <typename T> double projectionSquareNorm(T const & projectionMatrix) {
	return (projectionMatrix.transpose() * projectionMatrix).diagonal().sum();
}

template <typename T> void normalizeProjection(T & projectionMatrix) {
	projectionMatrix *= 1.0/sqrt(projectionSquareNorm(projectionMatrix));
}


template<typename T> void RandomMatrixInit(boost::mt19937 & rng, Eigen::MatrixBase< T>& mat, double mean, double sigma) {
	using namespace boost;
	normal_distribution<> distrib(mean,sigma);
	variate_generator<mt19937&, normal_distribution<> > rndGen(rng, distrib);
	for(int j=0; j<mat.cols(); j++)
		for(int i=0; i<mat.rows(); i++)
			mat(i,j) = rndGen();
}

inline bool almostEqual(double x, double y, double leeway=1.0) {
	double diff = fabs(x-y);
	double sum = fabs(x+y);
	return diff <= leeway*std::numeric_limits<double>::epsilon()*sum;
}


inline void randomProjectionMatrix(boost::mt19937 & rngParams, PMatrix & mat) {
	RandomMatrixInit(rngParams,mat,0.0,1.0);
	Eigen::JacobiSVD<PMatrix> svd(mat, Eigen::ComputeThinU | Eigen::ComputeThinV);
	if(mat.rows()>mat.cols())
		mat.noalias() = svd.matrixU();
	else
		mat.noalias() = svd.matrixV().transpose();
#ifndef NDEBUG
	for(int r=0;r<mat.rows();r++){
		for(int r0=0;r0<mat.rows();r0++){
			double dotprod = mat.row(r).dot(mat.row(r0));
			if(r==r0)
				assert(fabs(dotprod-1.0) <= std::numeric_limits<double>::epsilon()*mat.cols());
			else 
				assert(fabs(dotprod) <= std::numeric_limits<double>::epsilon()*mat.cols());
		}
	}
#endif
}


template <typename T> T randomOrthogonalMatrix(boost::mt19937 & rngParams, int dims) {
	T P(dims,dims);
	double Pdet = 0.0;
	while(!(fabs(Pdet) >0.1 && fabs(Pdet) < 10)) {
		RandomMatrixInit(rngParams, P, 0, 1.0);
		Pdet = P.determinant();
		if(Pdet == 0.0) continue;//exceedingly unlikely.
		Eigen::HouseholderQR<Eigen::MatrixXd> qrOfP(P);
		P = qrOfP.householderQ();
		Pdet = P.determinant();
		if(Pdet < 0.0) {
			P.col(0) *=-1;
			Pdet = P.determinant();
		}
	}
	return P;
}

template <typename T> T randomUnscalingMatrix(boost::mt19937 & rngParams, int dims) {
	T P(dims, dims);
	double Pdet = 0.0;
	while(!(Pdet >0.1 &&Pdet < 10)) {
		RandomMatrixInit(rngParams, P, 0, 1.0);
		Pdet = P.determinant();
		assert(Pdet!=0);
		if( fabs(Pdet) <= std::numeric_limits<double>::epsilon()) continue;//exceedingly unlikely.
		
		if(Pdet < 0.0) //sign doesn't _really_ matter.
			P.col(0) *=-1;
		double scale= pow (fabs(Pdet),-1.0/double(dims));
		assert(scale==scale);

		P *= scale;
		Pdet = P.determinant();
	}
	return P;
}

template <typename T> void projectionRandomizeUniformScaled(boost::mt19937 & randGen, T & projectionMatrix) { //initializes all coefficients randomly to -1..1, then normalizes.
	boost::uniform_01<boost::mt19937> uniform01_rand(randGen);

	for(int col=0; col < projectionMatrix.cols(); col++)
		for(int row=0; row < projectionMatrix.rows(); row++) //column-major storage
			projectionMatrix(row,col) = uniform01_rand()*2.0-1.0;
		
	normalizeProjection(projectionMatrix);
}


template<typename arrayT>
void shuffle(boost::mt19937 & randGen, arrayT arr, size_t size){
	for(size_t i = 0; i<size;++i)
		swap(arr[i],arr[i+randGen() %(size-i)]);
}

// (Slower) alternative is something like:
//	random_shuffle(start, end, shuffle_rnd_helper(randGen) );
//
//struct shuffle_rnd_helper {
//	boost::mt19937 & randGen;
//	shuffle_rnd_helper(boost::mt19937 & randGen) : randGen(randGen) {}
//	int operator()(int max) {return randGen()%max;}
//};

//[& randGen](int max) -> int {return randGen()%max;}

Eigen::MatrixXd shuffleMatrixCols(boost::mt19937 & randGen, Eigen::MatrixXd const & src);

#ifdef _MSC_VER
#define FOREACH(RANGEVAR_DECL, ITERATOR) for each(RANGEVAR_DECL in ITERATOR)
#else
#define FOREACH(RANGEVAR_DECL, ITERATOR) for(RANGEVAR_DECL : ITERATOR)
#endif

//#pragma managed(pop)
#ifdef _MSC_VER
#define isfinite(x)     (_finite(x)) 
#else
inline bool isfinite(double x) {return !(std::isinf(x)  || std::isnan(x));}
#endif
