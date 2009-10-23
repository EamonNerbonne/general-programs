#include "stdafx.h"
#include "EasyLvqTest.h"
#include "LvqDataSet.h"
#include "G2mLvqModel.h"
#include "GsmLvqModel.h"
#include "GmLvqModel.h"

using boost::mt19937;
using boost::normal_distribution;
using boost::variate_generator;
USING_PART_OF_NAMESPACE_EIGEN

	template<typename T>
void rndSet(mt19937 & rng, T& mat,double mean, double sigma) {
	normal_distribution<> distrib(mean,sigma);
	variate_generator<mt19937&, normal_distribution<> > rndGen(rng, distrib);
	for(int j=0; j<mat.cols();j++)
		for(int i=0;i<mat.rows();i++)
			mat(i,j) = rndGen();
}

#if NDEBUG
#define DIMS 32
#define POINTS 10000
#define ITERS 100
#else
#define DIMS 32
#define POINTS 1000
#define ITERS 10
#endif


void EigenBench() {
	double sink=0;
	mt19937 rndGen(37);
	Matrix<double,2,Eigen::Dynamic> A(2,DIMS);
	VectorXd tmp(DIMS);
	Vector2d tmp2;
	MatrixXd points(DIMS,POINTS);
	rndSet(rndGen, A, 0, 1.0);
	rndSet(rndGen, points, 0, 1.0);
	for(int i=0;i<ITERS;i++) {
		tmp.setZero();
		for(int k=0;k<POINTS;k++) {
			tmp2 = (A * points.col(k)).lazy();
			tmp += (A.transpose() * tmp2).lazy();
		}
		sink += tmp.sum();
	}

	cout << sink << endl;
}

unsigned int secure_rand() {
	unsigned int retval;
	rand_s(&retval);
	return retval;
}

LvqDataSet* ConstructDataSet(mt19937 & rndGen) {
	MatrixXd pAtrans(DIMS,DIMS);
	MatrixXd pBtrans(DIMS,DIMS);
	VectorXd offsetA(DIMS);
	VectorXd offsetB(DIMS);

	MatrixXd pointsA(DIMS,POINTS);
	MatrixXd pointsB(DIMS,POINTS);

	rndSet(rndGen, pAtrans, 0, 1.0);
	rndSet(rndGen, pBtrans, 0, 1.0);
	rndSet(rndGen, pointsA, 0, 1.0);
	rndSet(rndGen, pointsB, 0, 1.0);
	rndSet(rndGen, offsetA, 0, 1.0);
	rndSet(rndGen, offsetB, 0, 1.0);

	pointsA = pAtrans * pointsA + offsetA * VectorXd::Ones(POINTS).transpose();
	pointsB = pBtrans * pointsB + offsetB * VectorXd::Ones(POINTS).transpose();

	MatrixXd allpoints(DIMS, pointsA.cols()+pointsB.cols());
	allpoints.block(0,0,DIMS,pointsA.cols()) = pointsA;
	allpoints.block(0,pointsA.cols(),DIMS,pointsB.cols()) = pointsB;

	vector<int> trainingLabels(allpoints.cols());
	for(int i=0; i<(int)trainingLabels.size(); ++i)
		trainingLabels[i] = i/POINTS;

	return new LvqDataSet(allpoints, trainingLabels, 2); //2: 2 classes.
}

template <class T> void TestModel(mt19937 & rndGen, LvqDataSet * dataset, vector<int> const & protoDistrib, int iters) {
	using boost::scoped_ptr;
	using boost::progress_timer;
	scoped_ptr<AbstractLvqModel> model;
	{ 
		progress_timer t;
		model.reset(new T(protoDistrib, dataset->ComputeClassMeans()));
		cout<<"constructing "<<typeid(T).name()<<": ";
	}

	std::cout << "Before training: "<<dataset->ErrorRate(model.get())<< std::endl;

	{
		progress_timer t;
		for(int i=0;i<10;i++) {
			dataset->TrainModel(iters, rndGen, model.get() );
			std::cout << "After training for "<< model->trainIter <<" iterations: "<<dataset->ErrorRate(model.get())<< std::endl;
		}
		cout<<"training "<<typeid(T).name()<<": ";
	}
}

void EasyLvqTest() {
	using boost::scoped_ptr;

	mt19937 rndGen(347);
	rndGen.seed(secure_rand);

	scoped_ptr<LvqDataSet> dataset(ConstructDataSet(rndGen)); //2: 2 classes.

	vector<int> protoDistrib;
	for(int i=0;i<2;++i)
		protoDistrib.push_back(1);

   TestModel<GmLvqModel>(rndGen,  dataset.get(), protoDistrib, (ITERS + DIMS -1)/DIMS);
   TestModel<G2mLvqModel>(rndGen, dataset.get(), protoDistrib, ITERS);
   TestModel<GsmLvqModel>(rndGen, dataset.get(), protoDistrib, ITERS);
}