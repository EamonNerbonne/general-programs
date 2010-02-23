#include "stdafx.h"
#include "EasyLvqTest.h"
#include "LvqDataSet.h"
#include "G2mLvqModel.h"
#include "GsmLvqModel.h"
#include "GmLvqModel.h"
#include "DataSetUtils.h"

using boost::mt19937;
using boost::normal_distribution;
using boost::variate_generator;
USING_PART_OF_NAMESPACE_EIGEN


#define MEANSEP 1.9
#define DETERMINISTIC_SEED 
#define DETERMINISTIC_ORDER 

#if NDEBUG
#define DIMS 50
#define POINTS 50000
#define ITERS 5
#define CLASSCOUNT 3
#define PROTOSPERCLASS 1
#else
#define DIMS 16
#define POINTS 100
#define ITERS 5
#define CLASSCOUNT 3
#define PROTOSPERCLASS 2
#endif

#ifndef _MSC_VER
#include<cstdlib>

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

void PrintModelStatus(char const * label,AbstractLvqModel const * model,LvqDataSet const * dataset) {
	using namespace std;
	cout << label<< ": "<<dataset->ErrorRate(model);
	if(dynamic_cast<AbstractProjectionLvqModel const*>(model)) 
		cout<<"   [norm: "<< dynamic_cast<AbstractProjectionLvqModel const*>(model)->projectionNorm() <<"]";
	cout<<endl;
}

template <class T> void TestModel(mt19937 & rndGenOrig, bool randInit, LvqDataSet * dataset, vector<int> const & protoDistrib, int iters) {
	mt19937 rndGenCopy = rndGenOrig;

	mt19937 rndGen(rndGenCopy); //we do this to avoid changing the original rng, so we can rerun tests with the same sequence of random numbers generated.

	using boost::scoped_ptr;
	using boost::progress_timer;
	scoped_ptr<AbstractLvqModel> model;
	{ 
		progress_timer t;
		model.reset(new T(rndGen, randInit, protoDistrib, dataset->ComputeClassMeans()));
		cout<<"constructing "<<typeid(T).name()<<" ";
		if(randInit)
			cout<<"(random proj. init)";
		else
			cout<<"(identity proj. init)";
	}

	PrintModelStatus("Initial", model.get(), dataset);

	{
		progress_timer t;
		int num_groups=5;
		for(int i=0;i<num_groups;i++) {
			int itersDone=iters*i/num_groups;
			int itersUpto=iters*(i+1)/num_groups;
			int itersTodo = itersUpto-itersDone;
			if(itersTodo>0) {
				dataset->TrainModel(iters, rndGen, model.get() );
				PrintModelStatus("Trained",model.get(),dataset);
			}
		}
		cout<<"training "<<typeid(T).name()<<": ";
	}
}

void EasyLvqTest() {
#if EIGEN3
	cout<< "[EIGEN3]";
#endif
#if EIGEN_DONT_VECTORIZE
	cout<<"[NOVECTOR]";
#endif
	cout<<endl;
	using boost::scoped_ptr;

	mt19937 rndGen(347);
	mt19937 rndGen2(37); //347: 50%, 37:
#ifndef DETERMINISTIC_SEED
	rndGen.seed(secure_rand);
#endif
#ifndef DETERMINISTIC_ORDER
	rndGen2.seed(secure_rand);
#endif

	scoped_ptr<LvqDataSet> dataset(DataSetUtils::ConstructDataSet(rndGen, DIMS, POINTS,CLASSCOUNT,MEANSEP )); 

	vector<int> protoDistrib;
	for(int i=0;i<CLASSCOUNT;++i)
		protoDistrib.push_back(PROTOSPERCLASS);

   //TestModel<GmLvqModel>(rndGen2, true,  dataset.get(), protoDistrib, (ITERS + DIMS -1)*3/2/DIMS);
   //TestModel<GmLvqModel>(rndGen2, false,  dataset.get(), protoDistrib, (ITERS + DIMS -1)*3/2/DIMS);

   TestModel<G2mLvqModel>(rndGen2, true, dataset.get(), protoDistrib, ITERS);
   //TestModel<G2mLvqModel>(rndGen2, false, dataset.get(), protoDistrib, ITERS);

   //TestModel<GsmLvqModel>(rndGen2, true, dataset.get(), protoDistrib, ITERS);
   //TestModel<GsmLvqModel>(rndGen2, false, dataset.get(), protoDistrib, ITERS);
}