#include "stdafx.h"

#include <boost/random/mersenne_twister.hpp>
#include <boost/scoped_ptr.hpp>
#include "LvqModelSettings.h"

#include "CreateDataset.h"
#include "LvqDataset.h"
#include "PCA.h"
#include "G2mLvqModel.h"

#include <bench/BenchTimer.h>

using boost::mt19937;
using std::cout;
using std::cerr;
using boost::scoped_ptr;
using std::vector;

#define DIMS 32
#define CLASSES 4
#define POINTS_PER_CLASS 1000
#define MEANSEP 5.0
#define PROTOSPERCLASS 3
#define FOLDS 5

#define PRINTLOG 1

#if PRINTLOG
#define LOG(X) (cout<<X)
#else
#define LOG(X) 0;
#endif


BOOST_AUTO_TEST_CASE( nn_test )
{
	mt19937 rng(1337);
	scoped_ptr<LvqDataset> dataset(CreateDataset::ConstructGaussianClouds(rng,rng,DIMS, CLASSES,POINTS_PER_CLASS,MEANSEP));
	dataset->shufflePoints(rng);

	vector<int> protoDistrib;
	for(int i=0;i<CLASSES;++i)
		protoDistrib.push_back(PROTOSPERCLASS);
	LOG("Random guess accuracy: "<<(1.0-1.0/CLASSES)<<"\n");

	Matrix_P checkerbox(2,DIMS);
	//Vector_2::LinSpaced(2,0,1) * Vector_N::Ones(DIMS).transpose() + Vector_2::Ones() * Vector_N::LinSpaced(DIMS,0,DIMS-1).transpose()
	for(int j=0;j<DIMS;++j)
		for(int i=0;i<LVQ_LOW_DIM_SPACE;++i) 
			checkerbox(i,j) = (LvqFloat)((i+j+1)%2);

	BenchTimer timeRawNN,timePca,timePcaNN, timeG2m;
	for(int fold =0;fold<FOLDS;++fold) {
		vector<int> testSet(dataset->GetTestSubset(fold,FOLDS));
		vector<int> trainingSet(dataset->GetTrainingSubset(fold,FOLDS));

		timeRawNN.start();
		double rawErrorRate = dataset->NearestNeighborErrorRate(trainingSet,dataset.get(),testSet);
		timeRawNN.stop();

		LOG("Raw: "<<rawErrorRate);

		timeG2m.start();
		G2mLvqModel model(as_lvalue(LvqModelSettings(LvqModelSettings::AutoModelType,rng,rng,protoDistrib,dataset.get(),trainingSet)));
		LvqModel::Statistics stats;
		dataset->TrainModel(25, &model, &stats, trainingSet, dataset.get(), testSet,nullptr,false);
		timeG2m.stop();

		LvqDatasetStats statsG2m = dataset->ComputeCostAndErrorRate(testSet,&model);
		LOG(", G2m: "<<statsG2m.errorRate());

		double g2mNNrate = dataset->NearestNeighborProjectedErrorRate(trainingSet,dataset.get(),testSet, model.projectionMatrix() );
		LOG(", G2mNN: "<<g2mNNrate );

		timePca.start();
		Matrix_P transform= PcaProjectInto2d(dataset->ExtractPoints(trainingSet) );
		timePca.stop();

		timePcaNN.start();
		double pcaErrorRate = dataset->NearestNeighborProjectedErrorRate(trainingSet,dataset.get(),testSet,transform);
		timePcaNN.stop();

		BOOST_CHECK(pcaErrorRate >= rawErrorRate);
		LOG(", PcaNN: "<<pcaErrorRate );

		double identTransRate = dataset->NearestNeighborProjectedErrorRate(trainingSet,dataset.get(),testSet, checkerbox);
		BOOST_CHECK(identTransRate >= pcaErrorRate);
		BOOST_CHECK(identTransRate >= g2mNNrate);
		LOG(", identNN: "<<identTransRate <<"\n");
	}

	LOG("RawNN time: "<<timeRawNN.best()<<", Pca time: "<<timePca.best()<<", PcaNN time: "<<timePcaNN.best() <<", G2m time: "<<timeG2m.best() <<"\n\n");
}