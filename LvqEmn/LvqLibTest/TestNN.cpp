#include "stdafx.h"
#include <boost/random/mersenne_twister.hpp>
#include <boost/scoped_ptr.hpp>
#include "DatasetUtils.h"
#include "PCA.h"
#include "G2mLvqModel.h"

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

#define PRINTLOG 0

#if PRINTLOG
#define LOG(X) (cout<<X)
#else
#define LOG(X) 0;
#endif

template <typename T> T & temp(T && temporary_value) {return temporary_value;}

BOOST_AUTO_TEST_CASE( nn_test )
{
	mt19937 rng(1337);
	scoped_ptr<LvqDataset> dataset(DatasetUtils::ConstructGaussianClouds(rng,rng,DIMS, CLASSES,POINTS_PER_CLASS,MEANSEP));
	dataset->shufflePoints(rng);

	vector<int> protoDistrib;
	for(int i=0;i<CLASSES;++i)
		protoDistrib.push_back(PROTOSPERCLASS);
	LOG("Random guess accuracy: "<<(1.0-1.0/CLASSES)<<"\n");

	PMatrix checkerbox(2,DIMS);
	//Vector2d::LinSpaced(2,0,1) * VectorXd::Ones(DIMS).transpose() + Vector2d::Ones() * VectorXd::LinSpaced(DIMS,0,DIMS-1).transpose()
	for(int j=0;j<DIMS;++j)
		for(int i=0;i<LVQ_LOW_DIM_SPACE;++i) 
			checkerbox(i,j) = (i+j+1)%2;

	BenchTimer timeRawNN,timePca,timePcaNN, timeG2m;
	for(int fold =0;fold<FOLDS;++fold) {
		vector<int> testSet(dataset->GetTestSubset(fold,FOLDS));
		vector<int> trainingSet(dataset->GetTrainingSubset(fold,FOLDS));

		timeRawNN.start();
		double rawErrorRate = dataset->NearestNeighborErrorRate(trainingSet,dataset.get(),testSet);
		timeRawNN.stop();

		LOG("Raw: "<<rawErrorRate);

		timeG2m.start();
		G2mLvqModel model(temp(LvqModelSettings(LvqModelSettings::AutoModelType,rng,rng,protoDistrib,dataset->ComputeClassMeans(trainingSet))));
		dataset->TrainModel(25,&model,trainingSet,dataset.get() ,testSet);
		timeG2m.stop();

		double ignore, g2mRate;
		dataset->ComputeCostAndErrorRate(testSet,&model,ignore,g2mRate);
		LOG(",  G2m: "<<g2mRate );

		double g2mNNrate = dataset->NearestNeighborErrorRate(trainingSet,dataset.get(),testSet, model.projectionMatrix() );
		LOG(",  G2mNN: "<<g2mNNrate );

		timePca.start();
		PMatrix transform= PcaProjectInto2d(dataset->ExtractPoints(trainingSet) );
		timePca.stop();

		timePcaNN.start();
		double pcaErrorRate = dataset->NearestNeighborErrorRate(trainingSet,dataset.get(),testSet,transform);
		timePcaNN.stop();

		BOOST_CHECK(pcaErrorRate >= rawErrorRate);
		LOG(",  PcaNN: "<<pcaErrorRate );

		double identTransRate = dataset->NearestNeighborErrorRate(trainingSet,dataset.get(),testSet, checkerbox);
		BOOST_CHECK(identTransRate >= pcaErrorRate);
		BOOST_CHECK(identTransRate >= g2mNNrate);
		LOG(",  identNN: "<<identTransRate <<"\n");
		
	}

	LOG("RawNN time: "<<timeRawNN.best()<<", Pca time: "<<timePca.best()<<", PcaNN time: "<<timePcaNN.best() <<", G2m time: "<<timeG2m.best() <<"\n\n");
}