#include "stdafx.h"
#include "EasyLvqTest.h"
#include "LvqTypedefs.h"
//#include "LvqDataset.h"
//#include "G2mLvqModel.h"
//#include "GmLvqModel.h"
//#include "LgmLvqModel.h"
//#include "CreateDataset.h"
#include "LvqLib.h"
#include "utils.h"
#include <vector>

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
#define BENCH_RUNS 10
#define DIMS 31
#define POINTS_PER_CLASS 3000
#define ITERS 40
#define CLASSCOUNT 3
#define PROTOSPERCLASS 1
#else
#define BENCH_RUNS 3
#define DIMS 31
#define POINTS_PER_CLASS 300
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

	auto stats = ComputeCostAndErrorRate(dataset,model);

	cerr << label<< ": "<<stats.errorRate << ", "<<stats.meanCost;
	if(IsProjectionModel(model)) {
		auto shape = GetModelShape(model);
		Matrix_2N protos(2, shape.pointCount);
		ProjectPrototypes(model,protos.data());


		Vector_2 minV= protos.rowwise().minCoeff();
		Vector_2 maxV= protos.rowwise().maxCoeff();
		Vector_2 range = maxV-minV;
		minV-=range;
		maxV+=range;

		ClassDiagramT diagram(800,800);
		ClassBoundaries(model,minV(0),maxV(0),minV(1),maxV(1),(int)diagram.cols(),(int)diagram.rows(),diagram.data());
		Matrix_P projMatrix(2,shape.dimCount);
		GetProjectionMatrix(model,projMatrix.data());
		cerr<<" [norm: "<< projectionSquareNorm(projMatrix) <<"]"<<diagram.cast<unsigned>().sum()<<";";
	}
	cerr<<endl;
}

void TestModel(LvqModelType modelType, unsigned seed, bool useNgUpdate, LvqDataset const * dataset, int protosPerClass, int iters) {
	Eigen::BenchTimer t;
	GetDataShape(dataset);
	LvqModelSettingsRaw settings = defaultLvqModelSettings;
	settings.ModelType = modelType;
	settings.PrototypesPerClass = protosPerClass;
	settings.NgUpdateProtos = useNgUpdate;
	settings.ParamsSeed = seed;
	settings.InstanceSeed = seed;
	t.start();
	LvqModel* model= CreateLvqModel(settings, dataset,0);
	t.stop();

	cerr<<"constructing "
		<< (modelType==LgmModelType? "Lgm":modelType==GmModelType?"Gm":modelType==G2mModelType?"G2m":modelType==GpqModelType?"Gpq":"Ggm" )
		<<(useNgUpdate?" (NG update)":"")<<": "<<t.value()<<"s\n";

	PrintModelStatus("Initial", model, dataset);

	t.start();
	int num_groups=3;
	for(int i=0;i<num_groups;i++) {
		int itersDone=iters*i/num_groups;
		int itersUpto=iters*(i+1)/num_groups;
		int itersTodo = itersUpto-itersDone;
		if(itersTodo>0) {
			TrainModel(dataset,dataset,model,itersTodo,nullptr,nullptr,nullptr,false);
			PrintModelStatus("Trained",model, dataset);
		}
	}
	t.stop();
	cerr<<"training "
		<< (modelType==LgmModelType? "Lgm":modelType==GmModelType?"Gm":modelType==G2mModelType?"G2m":modelType==GpqModelType?"Gpq":"Ggm" )
		<<": "<<t.value()<<"s\n";
	FreeModel(model);
}

void EasyLvqTest() {

	Eigen::BenchTimer t, tLgm, tG2m, tGm,tGgm;
	LvqDataset* dataset = CreateGaussianClouds(37,37, DIMS, CLASSCOUNT*POINTS_PER_CLASS, CLASSCOUNT, MEANSEP); 

	for(int bI=0;bI<BENCH_RUNS;++bI)
	{
		t.start();
		tLgm.start();
		TestModel(LgmModelType, 0, false,  dataset, PROTOSPERCLASS, 2*(ITERS + DIMS -1)/DIMS);
		tLgm.stop();

		tGm.start();
		TestModel(GmModelType, 2, false, dataset, PROTOSPERCLASS, ITERS);
		tGm.stop();

		tG2m.start();
		TestModel(G2mModelType, 1, false, dataset, PROTOSPERCLASS, ITERS);
		tG2m.stop();


		tGgm.start();
		TestModel(GgmModelType, 3, false, dataset, PROTOSPERCLASS, ITERS);
		tGgm.stop();

		cerr<<"\n";
		t.stop();
	}

	FreeDataset(dataset);
	cout.precision(3);
	cout<<t.best()<<"s ("<<tLgm.best()<<", "<<tGm.best()<<", "<<tG2m.best()<<", "<<tGgm.best()<<")";
}