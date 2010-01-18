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


#define MEANSEP 2.0
#define DETERMINISTIC_SEED 
//#define DETERMINISTIC_ORDER 

#if NDEBUG
#define DIMS 25
#define POINTS 10000
#define ITERS 20
#define CLASSCOUNT 3
#define PROTOSPERCLASS 2
#else
#define DIMS 16
#define POINTS 100
#define ITERS 5
#define CLASSCOUNT 3
#define PROTOSPERCLASS 2
#endif

unsigned int secure_rand() {
	unsigned int retval;
	rand_s(&retval);
	return retval;
}

void PrintModelStatus(char const * label,AbstractLvqModel const * model,LvqDataSet const * dataset) {
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

   TestModel<GmLvqModel>(rndGen2,  dataset.get(), protoDistrib, (ITERS + DIMS -1)/DIMS);
   TestModel<G2mLvqModel>(rndGen2, dataset.get(), protoDistrib, ITERS);
   TestModel<GsmLvqModel>(rndGen2, dataset.get(), protoDistrib, ITERS);
}