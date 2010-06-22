#include "stdafx.h"
#include "EasyLvqTest.h"
#include "LvqDataset.h"
#include "G2mLvqModel.h"
#include "GsmLvqModel.h"
#include "GmLvqModel.h"
#include "DatasetUtils.h"

using boost::mt19937;
using boost::normal_distribution;
using boost::variate_generator;
using std::cerr;
using std::cout;
using namespace Eigen;
#define MEANSEP 1.9
#define DETERMINISTIC_SEED 
#define DETERMINISTIC_ORDER 

#if NDEBUG
//#define BENCH_RUNS 5
//#define DIMS 21
//#define POINTS_PER_CLASS 500
//#define ITERS 20
#define BENCH_RUNS 10
#define DIMS 31
#define POINTS_PER_CLASS 3000
#define ITERS 40
#define CLASSCOUNT 3
#define PROTOSPERCLASS 1
#else
#define BENCH_RUNS 3
#define DIMS 32
#define POINTS_PER_CLASS 100
#define ITERS 40
#define CLASSCOUNT 3
#define PROTOSPERCLASS 1
#endif

#ifndef _MSC_VER
#include<cstdlib>
#include<ctime>

unsigned int secure_rand() {
	static int rand_init =0;
	if(!rand_init)
		srand(time(0)); 
	return rand();
}
#else
unsigned int secure_rand() {
	unsigned int retval;
	rand_s(&retval);
	return retval;
}
#endif

void PrintModelStatus(char const * label,LvqModel const * model,LvqDataset const * dataset) {
	using namespace std;
	double meanCost=0,errorRate=0;
	dataset->ComputeCostAndErrorRate(dataset->GetTrainingSubset(0,0),model,meanCost,errorRate);

	cerr << label<< ": "<<errorRate << ", "<<meanCost;
	if(dynamic_cast<LvqProjectionModel const*>(model)) 
		cerr<<"   [norm: "<< dynamic_cast<LvqProjectionModel const*>(model)->projectionNorm() <<"]";
	cerr<<endl;
}

template <class T> void TestModel(mt19937 & rndGenOrig, bool randInit, LvqDataset const  * dataset, vector<int> const & protoDistrib, int iters) {
	Eigen::BenchTimer t;
	mt19937 rndGenCopy = rndGenOrig;
	mt19937 rndGen(rndGenCopy); //we do this to avoid changing the original rng, so we can rerun tests with the same sequence of random numbers generated.

	using boost::scoped_ptr;
	scoped_ptr<LvqModel> model;
	t.start();
	model.reset(new T(rndGen,rndGen, randInit, protoDistrib, dataset->ComputeClassMeans(dataset->GetTrainingSubset(0,0))));
	t.stop();
	cerr<<"constructing "<<typeid(T).name()<<" ("<<(randInit?"random":"identity")<<" proj. init)"<<t.value()<<"s\n";

	PrintModelStatus("Initial", model.get(), dataset);

	t.start();
	int num_groups=3;
	for(int i=0;i<num_groups;i++) {
		int itersDone=iters*i/num_groups;
		int itersUpto=iters*(i+1)/num_groups;
		int itersTodo = itersUpto-itersDone;
		if(itersTodo>0) {
			dataset->TrainModel(itersTodo, model.get(),dataset->GetTrainingSubset(0,0), 0,vector<int>() );
			PrintModelStatus("Trained",model.get(),dataset);
		}
	}
	t.stop();
	cerr<<"training "<<typeid(T).name()<<": "<<t.value()<<"s\n";
}

void EasyLvqTest() {
	using boost::scoped_ptr;

	mt19937 rndGen(347);
	mt19937 rndGen2(37); //347: 50%, 37:
#ifndef DETERMINISTIC_SEED
	rndGen.seed(secure_rand);
#endif
#ifndef DETERMINISTIC_ORDER
	rndGen2.seed(secure_rand);
#endif

	vector<int> protoDistrib;
	for(int i=0;i<CLASSCOUNT;++i)
		protoDistrib.push_back(PROTOSPERCLASS);

	Eigen::BenchTimer t;
	scoped_ptr<LvqDataset> dataset(DatasetUtils::ConstructGaussianClouds(rndGen,rndGen, DIMS, CLASSCOUNT, POINTS_PER_CLASS, MEANSEP)); 
	
	for(int bI=0;bI<BENCH_RUNS;++bI)
	{
		t.start();
		TestModel<GmLvqModel>(rndGen2, true,  dataset.get(), protoDistrib, (ITERS + DIMS -1)/DIMS);
		TestModel<GmLvqModel>(rndGen2, false,  dataset.get(), protoDistrib, (ITERS + DIMS -1)/DIMS);

		//TestModel<G2mLvqModel>(rndGen2, true, dataset.get(), protoDistrib, ITERS);
		//TestModel<G2mLvqModel>(rndGen2, false, dataset.get(), protoDistrib, ITERS);

		//TestModel<GsmLvqModel>(rndGen2, true, dataset.get(), protoDistrib, ITERS);
		//TestModel<GsmLvqModel>(rndGen2, false, dataset.get(), protoDistrib, ITERS);

		cerr<<"\n";
		t.stop();
	}
	
	cout.precision(3);
	cout<<t.best()<<"s";
}