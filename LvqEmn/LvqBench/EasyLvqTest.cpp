#include "stdafx.h"
#include "EasyLvqTest.h"
#include "LvqDataSet.h"

using boost::mt19937;
using boost::normal_distribution;
using boost::variate_generator;
USING_PART_OF_NAMESPACE_EIGEN

	template<typename T>
void rndSet(mt19937 & rng, T mat,double mean, double sigma) {
	normal_distribution<> distrib(mean,sigma);
	variate_generator<mt19937&, normal_distribution<> > rndGen(rng, distrib);
	for(int j=0; j<mat.cols();j++)
		for(int i=0;i<mat.rows();i++)
			mat(i,j) = rndGen();
}

#define DIMS 50
#define POINTS 1000

void EasyLvqTest() {
	using std::vector;
	using boost::scoped_ptr;
	boost::mt19937 rndGen(42);

	MatrixXd pAtrans(DIMS,DIMS);
	MatrixXd pBtrans(DIMS,DIMS);
	VectorXd offsetA(DIMS);
	VectorXd offsetB(DIMS);

	MatrixXd pointsA(DIMS,POINTS);
	MatrixXd pointsB(DIMS,POINTS);

	rndSet(rndGen,pAtrans,0,1.0);
	rndSet(rndGen,pBtrans,0,1.0);
	rndSet(rndGen,pointsA,0,1.0);
	rndSet(rndGen,pointsB,0,1.0);
	rndSet(rndGen,offsetA,0,1.0);
	rndSet(rndGen,offsetB,0,1.0);

	pointsA = pAtrans * pointsA + offsetA * VectorXd::Ones(POINTS).transpose();
	pointsB = pBtrans * pointsB + offsetB * VectorXd::Ones(POINTS).transpose();


	MatrixXd allpoints(DIMS, pointsA.cols()+pointsB.cols());
	allpoints.block(0,0,DIMS,pointsA.cols()) = pointsA;
	allpoints.block(0,pointsA.cols(),DIMS,pointsB.cols()) = pointsB;

	vector<int> trainingLabels(allpoints.cols());
	for(int i=0; i<trainingLabels.size(); ++i)
		trainingLabels[i] = i/POINTS;

	scoped_ptr<LvqDataSet> dataset( new LvqDataSet(allpoints, trainingLabels, 2)); //2: 2 classes.

	vector<int> protoDistrib;
	for(int i=0;i<2;++i)
		protoDistrib.push_back(1);

	scoped_ptr<LvqModel> model(dataset->ConstructModel(protoDistrib));
   
	dataset->TrainModel(1,rndGen,*model);
}