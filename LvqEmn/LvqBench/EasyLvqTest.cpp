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

template<typename T> void rndSet(mt19937 & rng, T& mat,double mean, double sigma) {
	normal_distribution<> distrib(mean,sigma);
	variate_generator<mt19937&, normal_distribution<> > rndGen(rng, distrib);

	for(int j=0; j<mat.cols(); j++)
		for(int i=0; i<mat.rows(); i++)
			mat(i,j) = rndGen();
}

#define MEANSEP 2.0
#if NDEBUG
#define DIMS 40
#define POINTS 10000
#define ITERS 20
#define CLASSCOUNT 4
#define PROTOSPERCLASS 4
#else
#define DIMS 16
#define POINTS 100
#define ITERS 5
#define CLASSCOUNT 3
#define PROTOSPERCLASS 2
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

void PrintModelStatus(AbstractLvqModel const * model,LvqDataSet const * dataset, char const * label) {
	using namespace std;
	cout << label<< ": "<<dataset->ErrorRate(model);
	if(dynamic_cast<AbstractProjectionLvqModel const*>(model)) 
		cout<<"   [norm: "<< dynamic_cast<AbstractProjectionLvqModel const*>(model)->projectionNorm() <<"]";

	cout<<endl;
}


template <class T> void TestModel(mt19937  rndGenOrig, LvqDataSet * dataset, vector<int> const & protoDistrib, int iters) {
	mt19937 rndGen(rndGenOrig);
	using boost::scoped_ptr;
	using boost::progress_timer;
	scoped_ptr<AbstractLvqModel> model;
	{ 
		progress_timer t;
		model.reset(new T(protoDistrib, dataset->ComputeClassMeans()));
		cout<<"constructing "<<typeid(T).name()<<": ";
	}

	PrintModelStatus("Initial",model.get(),dataset);

	{
		progress_timer t;
		for(int i=0;i<10;i++) {
			dataset->TrainModel(iters, rndGen, model.get() );
			PrintModelStatus("Trained",model.get(),dataset);
		}
		cout<<"training "<<typeid(T).name()<<": ";
	}
}

void EasyLvqTest() {
	using boost::scoped_ptr;

	int classCount=CLASSCOUNT;
	int protosPerClass=PROTOSPERCLASS;

	mt19937 rndGen(347);
	mt19937 rndGen2(37); //347: 50%, 37:

	//rndGen.seed(secure_rand);

	scoped_ptr<LvqDataSet> dataset(ConstructDataSet(rndGen, classCount)); 

	vector<int> protoDistrib;
	for(int i=0;i<classCount;++i)
		protoDistrib.push_back(protosPerClass);

   //TestModel<GmLvqModel>(rndGen2,  dataset.get(), protoDistrib, (ITERS + DIMS -1)/DIMS);
   TestModel<G2mLvqModel>(rndGen2, dataset.get(), protoDistrib, ITERS);
   TestModel<GsmLvqModel>(rndGen2, dataset.get(), protoDistrib, ITERS);
}