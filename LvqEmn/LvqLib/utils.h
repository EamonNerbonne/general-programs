#pragma once
#include "stdafx.h"

template <typename T> T sqr(T val) {return val*val;}

void makeRandomOrder(boost::mt19937 & randGen, int*const toFill, int count);

template <typename T> double projectionSquareNorm(T const & projectionMatrix) {
#if EIGEN3
	return (projectionMatrix.transpose() * projectionMatrix).diagonal().sum();
#else
	return (projectionMatrix.transpose() * projectionMatrix).lazy().diagonal().sum();
#endif
}

template<typename T>  void RandomMatrixInit(boost::mt19937 & rng, Eigen::MatrixBase< T>& mat, double mean, double sigma) {
	using namespace boost;
	normal_distribution<> distrib(mean,sigma);
	variate_generator<mt19937&, normal_distribution<> > rndGen(rng, distrib);

	for(int j=0; j<mat.cols(); j++)
		for(int i=0; i<mat.rows(); i++)
			mat(i,j) = rndGen();
}

template <typename T>  T randomOrthogonalMatrix(boost::mt19937 & rngParams, int dims) {
	T P(dims,dims);
	double Pdet = 0.0;
	while(!(abs(Pdet) >0.1 && abs(Pdet) < 10)) {
		RandomMatrixInit(rngParams, P, 0, 1.0);
		Pdet = P.determinant();
		if(Pdet == 0.0) continue;//exceedingly unlikely.
		
		//cout<< "Determinant: "<<Pdet<<"\n";
		HouseholderQR<MatrixXd> qrOfP(P);
		P = qrOfP.householderQ();
		Pdet = P.determinant();
		if(Pdet < 0.0) {
			P.col(0) *=-1;
			Pdet = P.determinant();
		}

//		cout<<"New determinant: "<<Pdet<<endl;
	}
	return P;
}

template <typename T>  T randomUnscalingMatrix(boost::mt19937 & rngParams, int dims) {
	T P(dims, dims);
	double Pdet = 0.0;
	while(!(Pdet >0.1 &&Pdet < 10)) {
		RandomMatrixInit(rngParams, P, 0, 1.0);
		Pdet = P.determinant();
		if(Pdet == 0.0) continue;//exceedingly unlikely.
		cout<< "Determinant: "<<Pdet<<"\n";
		double scale= pow(abs(Pdet),-1.0/dims);
		if(Pdet < 0.0) {//sign doesn't _really_ matter.
			P.col(0) *=-1;
			Pdet = P.determinant();
		}
		cout<< "Scale: "<<scale<<"\n";
		P = scale*P;
		Pdet = P.determinant();
		cout<<"New determinant: "<<Pdet<<"\n";
	}
	return P;
}

template <typename T> void normalizeMatrix(T & projectionMatrix) {
	double norm = projectionSquareNorm(projectionMatrix);
	double scaleBy = 1.0 / sqrt(norm);
#if EIGEN3
	projectionMatrix *= scaleBy; 
#else
	projectionMatrix = (scaleBy * projectionMatrix).lazy(); //TODO:can't I just use the eigen3 path here?
#endif
}

using namespace Eigen;

template <typename T> void projectionRandomizeUniformScaled(boost::mt19937 & randGen, T & projectionMatrix) { //initializes all coefficients randomly to -1..1, then normalizes.
	boost::uniform_01<boost::mt19937> uniform01_rand(randGen);

	for(int col=0; col < projectionMatrix.cols(); col++)
		for(int row=0; row < projectionMatrix.rows(); row++)  //column-major storage
			projectionMatrix(row,col) = uniform01_rand()*2.0-1.0;
		
	normalizeMatrix(projectionMatrix);
}

