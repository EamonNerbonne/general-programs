#pragma once
#include <vector>
#include <random>
#include <Eigen/Core>
#include <tuple>
#include "LvqConstants.h"
#include "LvqTypedefs.h"
#include "copy_ptr.h"

struct LvqModelRuntimeSettings
{
    bool NoNnErrorRateTracking, neiP, scP, noKP, neiB, LocallyNormalize, wGMu, SlowK;
    int ClassCount;
    double MuOffset, LrScaleP, LrScaleB, LR0, LrScaleBad;
    copy_ptr<std::mt19937> RngIter;
    LvqModelRuntimeSettings(int classCount, std::mt19937& rngIter);
};

class LvqDataset;

struct LvqModelSettings
{
    bool Ppca, RandomInitialBorders, NGu, NGi, Popt, Bcov, LrPp;
    double decay, iterScaleFactor;

    int Dimensionality;

    enum LvqModelType { AutoModelType, LgmModelType, GmModelType, G2mModelType, GgmModelType, GpqModelType, LpqModelType, FgmModelType, NormalModelType, LgrModelType }; //TODO: use enum classes.
    LvqModelType ModelType;
    std::mt19937 RngParams;
    LvqModelRuntimeSettings RuntimeSettings;
    std::vector<int> PrototypeDistribution;
    LvqDataset const* Dataset;

    size_t ClassCount() const { return PrototypeDistribution.size(); }
    std::pair<Matrix_NN, Eigen::VectorXi> InitByClassMeans() const;
    std::pair<Matrix_NN, Eigen::VectorXi> InitByNg();
    std::pair<Matrix_NN, Eigen::VectorXi> InitProtosBySetting();
    std::tuple<Matrix_NN, Matrix_NN, Eigen::VectorXi> InitProtosAndProjectionBySetting();
    std::tuple<std::vector<Matrix_NN>, Matrix_NN, Eigen::VectorXi> InitProjectionProtosBySetting();
    std::tuple<Matrix_NN, Matrix_NN, Eigen::VectorXi> InitRelevanceProtosBySetting();
    std::tuple<Matrix_P, Matrix_NN, Eigen::VectorXi, std::vector<Matrix_22, Eigen::aligned_allocator<Matrix_22>> > InitProtosProjectionBoundariesBySetting();

    Matrix_NN pcaTransform() const;
    Matrix_NN initTransform();
    int PrototypeCount() const;

    LvqModelSettings(LvqModelType modelType, std::mt19937& rngParams, std::mt19937& rngIter, std::vector<int> protodistrib, LvqDataset const* dataset);
    ptrdiff_t InputDimensions() const;
    ptrdiff_t OutputDimensions() const { return Dimensionality == 0 ? LVQ_LOW_DIM_SPACE : Dimensionality; }

    template<typename T>
    void AssertModelIsOfRightType(T*) {
        if (ModelType == AutoModelType)
            ModelType = T::ThisModelType;
        else if (ModelType != T::ThisModelType) {
            std::cerr << "Invalid Model Type!\n";
            std::exit(10);
        }
    }

    LvqModelSettings& self() { return *this; }

private:
    void ProjInit(Matrix_NN const& prototypes, Matrix_NN& P);
};

struct LvqModel;

LvqModel* ConstructLvqModel(LvqModelSettings& initSettings);
