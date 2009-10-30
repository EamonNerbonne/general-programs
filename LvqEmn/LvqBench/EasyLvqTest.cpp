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

#define MEANSEP 5.0
#if NDEBUG
#define DIMS 32
#define POINTS 5000
#define ITERS 10
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

MatrixXd MakePointCloud(mt19937 & rndGen, int dims, int pointCount) {
	MatrixXd P(dims, dims);
	VectorXd offset(dims);

	MatrixXd points(dims,pointCount);

	rndSet(rndGen, P, 0, 1.0);
	rndSet(rndGen, points, 0, 1.0);
	rndSet(rndGen, offset, 0, MEANSEP);

	return P * points + offset * VectorXd::Ones(pointCount).transpose();
}


LvqDataSet* ConstructDataSet(mt19937 & rndGen, int numClasses) {
	MatrixXd pointsA = MakePointCloud(rndGen, DIMS, POINTS);
	MatrixXd pointsB = MakePointCloud(rndGen, DIMS, POINTS);

	MatrixXd allpoints(DIMS, numClasses*POINTS);
	for(int classLabel=0;classLabel < numClasses;classLabel++) {
		allpoints.block(0,classLabel*POINTS,DIMS,POINTS) = MakePointCloud(rndGen, DIMS, POINTS);;
	}

	vector<int> trainingLabels(allpoints.cols());
	for(int i=0; i<(int)trainingLabels.size(); ++i) 
		trainingLabels[i] = i/POINTS;

	return new LvqDataSet(allpoints, trainingLabels, numClasses); //2: 2 classes.
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

	int classCount=10;
	int protosPerClass=3;

	mt19937 rndGen;
	rndGen.seed(secure_rand);

	scoped_ptr<LvqDataSet> dataset(ConstructDataSet(rndGen, classCount)); 

	vector<int> protoDistrib;
	for(int i=0;i<classCount;++i)
		protoDistrib.push_back(protosPerClass);

   //TestModel<GmLvqModel>(rndGen,  dataset.get(), protoDistrib, (ITERS + DIMS -1)/DIMS);
   TestModel<G2mLvqModel>(rndGen, dataset.get(), protoDistrib, ITERS);
   //TestModel<GsmLvqModel>(rndGen, dataset.get(), protoDistrib, ITERS);
}