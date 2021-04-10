#include "stdafx.h"

#include <random>
#include "LvqModelSettings.h"

#include "CreateDataset.h"
#include "LvqDataset.h"
#include "PCA.h"
#include "G2mLvqModel.h"

#include <bench/BenchTimer.h>

using std::mt19937;
using std::cout;
using std::cerr;
using std::vector;
using std::unique_ptr;

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


BOOST_AUTO_TEST_CASE(nn_test)
{
    mt19937 rng(1337);
    unique_ptr<LvqDataset> dataset(CreateDataset::ConstructGaussianClouds(rng, rng, DIMS, CLASSES, POINTS_PER_CLASS, MEANSEP));
    dataset->shufflePoints(rng);

    vector<int> protoDistrib;
    for (int i = 0;i < CLASSES;++i)
        protoDistrib.push_back(PROTOSPERCLASS);
    LOG("Random guess accuracy: " << (1.0 - 1.0 / CLASSES) << "\n");

    Matrix_P checkerbox(2, DIMS);
    //Vector_2::LinSpaced(2,0,1) * Vector_N::Ones(DIMS).transpose() + Vector_2::Ones() * Vector_N::LinSpaced(DIMS,0,DIMS-1).transpose()
    for (int j = 0;j < DIMS;++j)
        for (int i = 0;i < LVQ_LOW_DIM_SPACE;++i)
            checkerbox(i, j) = (LvqFloat)((i + j + 1) % 2);

    BenchTimer timeRawNN, timePca, timePcaNN, timeG2m;
    for (int fold = 0;fold < FOLDS;++fold) {
        unique_ptr<LvqDataset> testSet(dataset->Extract(dataset->GetTestSubset(fold, FOLDS)));
        unique_ptr<LvqDataset> trainingSet(dataset->Extract(dataset->GetTrainingSubset(fold, FOLDS)));


        timeRawNN.start();
        double rawErrorRate = trainingSet->NearestNeighborErrorRate(*testSet);
        timeRawNN.stop();

        LOG("Raw: " << rawErrorRate);

        timeG2m.start();
        auto settings = LvqModelSettings(LvqModelSettings::AutoModelType, rng, rng, protoDistrib, trainingSet.get());
        G2mLvqModel model(settings);
        LvqModel::Statistics stats;
        trainingSet->TrainModel(25, model, &stats, testSet.get(), nullptr, false);
        timeG2m.stop();

        LvqDatasetStats statsG2m = testSet->ComputeCostAndErrorRate(model);
        LOG(", G2m: " << statsG2m.errorRate());

        double g2mNNrate = trainingSet->NearestNeighborProjectedErrorRate(*testSet, model.projectionMatrix());
        LOG(", G2mNN: " << g2mNNrate);

        timePca.start();
        Matrix_P transform = PcaProjectInto2d(trainingSet->getPoints());
        timePca.stop();

        timePcaNN.start();
        double pcaErrorRate = trainingSet->NearestNeighborProjectedErrorRate(*testSet, transform);
        timePcaNN.stop();

        BOOST_CHECK(pcaErrorRate >= rawErrorRate);
        LOG(", PcaNN: " << pcaErrorRate);

        double identTransRate = trainingSet->NearestNeighborProjectedErrorRate(*testSet, checkerbox);
        BOOST_CHECK(identTransRate >= pcaErrorRate);
        BOOST_CHECK(identTransRate >= g2mNNrate);
        LOG(", identNN: " << identTransRate << "\n");
    }

    LOG("RawNN time: " << timeRawNN.best() << ", Pca time: " << timePca.best() << ", PcaNN time: " << timePcaNN.best() << ", G2m time: " << timeG2m.best() << "\n\n");
}