#include "stdafx.h"
#include "LgmLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"
#include "SmartSum.h"
#include "randomProjectionMatrix.h"


static inline void NormalizeP(bool locally, vector<Matrix_NN >& P) {
    if (locally) {
        for (size_t i = 0;i < P.size();++i) normalizeProjection(P[i]);
    }
    else {
        double overallNorm = std::accumulate(P.begin(), P.end(), 0.0, [](double cur, Matrix_NN& mat)->double {
            double norm = mat.squaredNorm();
            /*if(norm > 1e100) {
                mat *= 1.0/sqrt(norm);
                norm = 1.0;
            }*/
            assert(isfinite_emn(norm));
            return cur + norm;
        });
        assert(isfinite_emn(overallNorm));
        double scale = 1.0 / sqrt(overallNorm / P.size());
        for (size_t i = 0;i < P.size();++i) P[i] *= scale;
    }
}


LgmLvqModel::LgmLvqModel(LvqModelSettings& initSettings)
    : LvqModel(initSettings)
    , totalMuJLr(0.0)
    , totalMuKLr(0.0)
    , tmpSrcDimsV1(initSettings.InputDimensions())
    , tmpSrcDimsV2(initSettings.InputDimensions())
    , tmpDestDimsV1()
    , tmpDestDimsV2()
{
    if (initSettings.Dimensionality == 0)
        initSettings.Dimensionality = (int)initSettings.InputDimensions();
    if (initSettings.Dimensionality < 0 || initSettings.Dimensionality >(int) initSettings.InputDimensions()) {
        std::cerr << "Dimensionality out of range\n";
        std::exit(10);
    }

    tmpDestDimsV1.resize(initSettings.Dimensionality);
    tmpDestDimsV2.resize(initSettings.Dimensionality);

    initSettings.AssertModelIsOfRightType(this);

    using namespace std;
    auto InitProtos = initSettings.InitProjectionProtosBySetting();
    auto initP = get<0>(InitProtos);

    Matrix_NN prototypes = get<1>(InitProtos);
    pLabel.resizeLike(get<2>(InitProtos));
    pLabel = get<2>(InitProtos);

    size_t protoCount = pLabel.size();
    prototype.resize(protoCount);
    P.resize(protoCount);

    for (size_t protoIndex = 0; protoIndex < protoCount; ++protoIndex) {
        prototype[protoIndex].resize(initSettings.InputDimensions());
        prototype[protoIndex] = prototypes.col(protoIndex);

        P[protoIndex].resizeLike(initP[protoIndex]);
        P[protoIndex] = initP[protoIndex];
    }

    NormalizeP(settings.LocallyNormalize, P);
}


MatchQuality LgmLvqModel::learnFrom(Vector_N const& trainPoint, int trainLabel) {

    using namespace std;

    GoodBadMatch matches = findMatches(trainPoint, trainLabel);
    double learningRate = stepLearningRate(matches.matchGood);
    double lr_point = settings.LR0 * learningRate;

    //now matches.good is "J" and matches.bad is "K".
    MatchQuality retval = matches.LvqQuality();

    if (!isfinite_emn(retval.muJ + retval.muK))
        return retval;

    double lr_mu_K2 = lr_point * 2.0 * retval.muK;
    double lr_mu_J2 = lr_point * 2.0 * retval.muJ;
    double lr_bad = (settings.SlowK ? sqr(1.0 - learningRate) : 1.0) * settings.LrScaleBad;

    int J = matches.matchGood;
    int K = matches.matchBad;

    Vector_N& vJ = tmpSrcDimsV1;
    Vector_N& vK = tmpSrcDimsV2;
    Vector_N& Pj_vJ = tmpDestDimsV1;
    Vector_N& Pk_vK = tmpDestDimsV2;

    vJ.noalias() = prototype[J] - trainPoint;
    vK.noalias() = prototype[K] - trainPoint;

    Pj_vJ.noalias() = P[J] * vJ;
    Pk_vK.noalias() = P[K] * vK;

    prototype[J].noalias() -= (lr_mu_J2) * (P[J].transpose() * Pj_vJ);
    prototype[K].noalias() -= (lr_bad * lr_mu_K2) * (P[K].transpose() * Pk_vK);

    P[J].noalias() -= (settings.LrScaleP * lr_mu_J2) * (Pj_vJ * vJ.transpose());
    P[K].noalias() -= (settings.LrScaleP * lr_mu_K2) * (Pk_vK * vK.transpose());


    if (settings.neiP) {
        if (settings.LocallyNormalize) {//optimization: only need to normalize changed projections.
            normalizeProjection(P[J]);
            normalizeProjection(P[K]);
        }
        else NormalizeP(settings.LocallyNormalize, P);
    }

    totalMuJLr += lr_point * retval.muJ;
    totalMuKLr -= lr_point * retval.muK;

    return retval;
}

MatchQuality LgmLvqModel::ComputeMatches(Vector_N const& unknownPoint, int pointLabel) const { return findMatches(unknownPoint, pointLabel).LvqQuality(); }

size_t LgmLvqModel::MemAllocEstimate() const {
    return
        sizeof(LgmLvqModel) + //base structure size
        sizeof(int) * pLabel.size() + //dyn.alloc labels
        (sizeof(double) * P[0].size() + sizeof(Matrix_NN)) * P.size() + //dyn alloc prototype transforms
        sizeof(double) * (tmpSrcDimsV1.size() + tmpSrcDimsV2.size() + tmpDestDimsV1.size() + tmpDestDimsV2.size()) + //various vector temps
        (sizeof(Vector_N) + sizeof(double) * prototype[0].size()) * prototype.size() +//dyn alloc prototypes
        (16 / 2) * (4 + prototype.size() * 2);//estimate for alignment mucking.
}

void LgmLvqModel::AppendTrainingStatNames(std::vector<std::wstring>& retval) const {
    LvqModel::AppendTrainingStatNames(retval);
    retval.push_back(L"Projection Norm Maximum!norm!Prototype Matrix");
    retval.push_back(L"Projection Norm Mean!norm!Prototype Matrix");
    retval.push_back(L"Projection Norm Minimum!norm!Prototype Matrix");

    retval.push_back(L"Cumulative \u03BC-J-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
    retval.push_back(L"Cumulative \u03BC-K-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
}

void LgmLvqModel::AppendOtherStats(std::vector<double>& stats, LvqDataset const* trainingSet, LvqDataset const* testSet) const {
    LvqModel::AppendOtherStats(stats, trainingSet, testSet);
    double minNorm = std::numeric_limits<double>::max();
    double maxNorm = 0.0;
    double normSum = 0.0;

    for (size_t i = 0;i < P.size();++i) {
        double norm = P[i].squaredNorm();
        if (norm < minNorm) minNorm = norm;
        if (norm > maxNorm) maxNorm = norm;
        normSum += norm;
    }

    stats.push_back(maxNorm);
    stats.push_back(normSum / P.size());
    stats.push_back(minNorm);

    stats.push_back(totalMuJLr);
    stats.push_back(totalMuKLr);
}

vector<int> LgmLvqModel::GetPrototypeLabels() const {
    vector<int> retval(prototype.size());
    for (unsigned i = 0;i < prototype.size();++i)
        retval[i] = pLabel[i];
    return retval;
}

void LgmLvqModel::DoOptionalNormalization() {
    if (!settings.neiP)
        NormalizeP(settings.LocallyNormalize, P);
}


Matrix_NN LgmLvqModel::PrototypeDistances(Matrix_NN const& points) const {
    Matrix_NN tmpPointsDiff(points.rows(), points.cols())
        , tmpPointsDiffProj(P[0].rows(), points.cols());
    Matrix_NN newPoints(prototype.size(), points.cols());
    for (size_t protoI = 0;protoI < prototype.size();++protoI) {
        tmpPointsDiff.noalias() = points.colwise() - prototype[protoI];
        tmpPointsDiffProj.noalias() = P[protoI] * tmpPointsDiff;
        newPoints.row(protoI).noalias() = tmpPointsDiffProj.colwise().squaredNorm();//not squaredNorm? is never going to matter, so I should use (faster) squaredNorm... oh well.
    }
    return newPoints;
}

Matrix_NN LgmLvqModel::GetCombinedTransforms() const {
    size_t totalRows = std::accumulate(P.cbegin(), P.cend(), size_t(0), [](size_t val, Matrix_NN const& p) { return val + p.rows(); });
    Matrix_NN retval(totalRows, P[0].cols());
    size_t rowInit = 0;
    std::for_each(P.cbegin(), P.cend(), [&rowInit, &retval](Matrix_NN const& p) {
        retval.middleRows(rowInit, p.rows()).noalias() = p;
        rowInit += p.rows();
    });
    return retval;
}
