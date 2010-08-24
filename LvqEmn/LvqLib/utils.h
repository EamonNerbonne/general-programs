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
	while(!(fabs(Pdet) >0.1 && fabs(Pdet) < 10)) {
		RandomMatrixInit(rngParams, P, 0, 1.0);
		Pdet = P.determinant();
		if(Pdet == 0.0) continue;//exceedingly unlikely.
		//cout<< "Determinant: "<<Pdet<<"\n";
		Eigen::HouseholderQR<Eigen::MatrixXd> qrOfP(P);
		P = qrOfP.householderQ();
		Pdet = P.determinant();
		if(Pdet < 0.0) {
			P.col(0) *=-1;
			Pdet = P.determinant();
		}
		//cout<<"New determinant: "<<Pdet<<endl;
	}
	return P;
}

template <typename T>  T randomUnscalingMatrix(boost::mt19937 & rngParams, int dims) {
	T P(dims, dims);
	double Pdet = 0.0;
	//std::cout<<"RND-mat"<< rngParams()<<","<<rngParams()<<","<<rngParams()<<"\n";
	while(!(Pdet >0.1 &&Pdet < 10)) {
		RandomMatrixInit(rngParams, P, 0, 1.0);
		Pdet = P.determinant();
		assert(Pdet!=0);
		DEBUGPRINT(Pdet);
		if(Pdet == 0.0)  continue;//exceedingly unlikely.
		
		if(Pdet < 0.0) //sign doesn't _really_ matter.
			P.col(0) *=-1;
		double scale= pow (fabs(Pdet),-1.0/double(dims));
		assert(scale==scale);

		P = scale*P;
		Pdet = P.determinant();
		assert(Pdet==Pdet);
		DEBUGPRINT(Pdet);
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



static int shuffle_rnd_helper(boost::mt19937 & randGen, int options) {
	return randGen()%options; //slightly biased since randGen generates random _bits_ and the highest modulo wrapping may not "fill" the last options batch.  This is very minor; I don't care.
}

template<typename iterT>
void shuffle(boost::mt19937 & randGen, iterT start, iterT end){
	using std::random_shuffle;
	using std::accumulate;
	using boost::bind;
	boost::function<int (int max)> rnd = bind(shuffle_rnd_helper, randGen, _1);

	random_shuffle(start, end, rnd);
}


Eigen::MatrixXd shuffleMatrixCols(boost::mt19937 & randGen, Eigen::MatrixXd const & src);