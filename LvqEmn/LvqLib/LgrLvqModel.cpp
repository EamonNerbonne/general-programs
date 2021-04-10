#include "StdAfx.h"
#include "LgrLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"
#include "SmartSum.h"
#include "randomProjectionMatrix.h"


inline void LgrLvqModel::NormalizeRelevances() {
    if (settings.LocallyNormalize) {
        for (size_t i = 0;i < prototype.size();++i) normalizeProjection(prototype[i].R);
    }
    else {
        double overallNorm = std::accumulate(prototype.begin(), prototype.end(), 0.0, [](double cur, Prototype& mat) -> double {
            double norm = mat.R.squaredNorm();
            /*if(norm > 1e100) {
            mat *= 1.0/sqrt(norm);
            norm = 1.0;
            }*/
            assert(isfinite_emn(norm));
            return cur + norm;
        });
        assert(isfinite_emn(overallNorm));
        double scale = 1.0 / sqrt(overallNorm / prototype.size());
        for (size_t i = 0;i < prototype.size();++i) prototype[i].R *= scale;
    }
}


LgrLvqModel::LgrLvqModel(LvqModelSettings& initSettings)
    : LvqModel(initSettings)
    , totalMuJLr(0.0)
    , totalMuKLr(0.0)
    , tmpV1(initSettings.InputDimensions())
    , tmpV2(initSettings.InputDimensions())
    , tmpV3(initSettings.InputDimensions())
    , tmpV4(initSettings.InputDimensions())
{
    initSettings.Dimensionality = (int)initSettings.InputDimensions();

    initSettings.AssertModelIsOfRightType(this);

    using namespace std;
    auto relevancesAndProtos = initSettings.InitRelevanceProtosBySetting();
    auto relevances = get<0>(relevancesAndProtos);
    auto protos = get<1>(relevancesAndProtos);

    pLabel.resize(get<2>(relevancesAndProtos).size());
    pLabel = get<2>(relevancesAndProtos);
    size_t protoCount = pLabel.size();


    for (size_t protoIndex = 0; protoIndex < protoCount; ++protoIndex) {
        Prototype proto;
        proto.R.resizeLike(relevances.col(protoIndex));
        proto.R = relevances.col(protoIndex);
        proto.pos.resizeLike(protos.col(protoIndex));
        proto.pos = protos.col(protoIndex);
        prototype.push_back(proto);
    }

    NormalizeRelevances();
}


MatchQuality LgrLvqModel::learnFrom(Vector_N const& trainPoint, int trainLabel) {

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

    Vector_N& vJ = tmpV1;
    Vector_N& vK = tmpV2;
    Vector_N& Pj_vJ = tmpV3;
    Vector_N& Pk_vK = tmpV4;

    vJ.noalias() = prototype[J].pos - trainPoint;
    vK.noalias() = prototype[K].pos - trainPoint;

    Pj_vJ.noalias() = prototype[J].R.asDiagonal() * vJ;
    Pk_vK.noalias() = prototype[K].R.asDiagonal() * vK;

    prototype[J].pos.noalias() -= (lr_mu_J2) * (prototype[J].R.asDiagonal() * Pj_vJ);
    prototype[K].pos.noalias() -= (lr_bad * lr_mu_K2) * (prototype[K].R.asDiagonal() * Pk_vK);

    prototype[J].R.noalias() -= (settings.LrScaleP * lr_mu_J2) * (Pj_vJ.array() * vJ.array()).matrix();
    prototype[K].R.noalias() -= (settings.LrScaleP * lr_mu_K2) * (Pk_vK.array() * vK.array()).matrix();


    if (settings.neiP) {
        if (settings.LocallyNormalize) {//optimization: only need to normalize changed projections.
            normalizeProjection(prototype[J].R);
            normalizeProjection(prototype[K].R);
        }
        else NormalizeRelevances();
    }

    totalMuJLr += lr_point * retval.muJ;
    totalMuKLr -= lr_point * retval.muK;

    return retval;
}

MatchQuality LgrLvqModel::ComputeMatches(Vector_N const& unknownPoint, int pointLabel) const { return findMatches(unknownPoint, pointLabel).LvqQuality(); }

size_t LgrLvqModel::MemAllocEstimate() const {
    return
        sizeof(LgrLvqModel) + //base structure size
        sizeof(int) * pLabel.size() + 16 / 2 + //dyn.alloc labels
        (
            sizeof(Vector_N) + sizeof(double) * prototype[0].pos.size() // one vector
            + (16 / 2) //alignment wastage guesstimate

            ) * (prototype.size() * 2 + 4)//dyn alloc prototypes + 4 temps
        ;//estimate for alignment mucking.
}

void LgrLvqModel::AppendTrainingStatNames(std::vector<std::wstring>& retval) const {
    LvqModel::AppendTrainingStatNames(retval);
    retval.push_back(L"Relevance Norm Maximum!norm!Prototype Relevance");
    retval.push_back(L"Relevance Norm Mean!norm!Prototype Relevance");
    retval.push_back(L"Relevance Norm Minimum!norm!Prototype Relevance");

    retval.push_back(L"Cumulative \u03BC-J-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
    retval.push_back(L"Cumulative \u03BC-K-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
}

void LgrLvqModel::AppendOtherStats(std::vector<double>& stats, LvqDataset const* trainingSet, LvqDataset const* testSet) const {
    LvqModel::AppendOtherStats(stats, trainingSet, testSet);
    double minNorm = std::numeric_limits<double>::max();
    double maxNorm = 0.0;
    double normSum = 0.0;

    for (size_t i = 0;i < prototype.size();++i) {
        double norm = prototype[i].R.squaredNorm();
        if (norm < minNorm) minNorm = norm;
        if (norm > maxNorm) maxNorm = norm;
        normSum += norm;
    }

    stats.push_back(maxNorm);
    stats.push_back(normSum / prototype.size());
    stats.push_back(minNorm);

    stats.push_back(totalMuJLr);
    stats.push_back(totalMuKLr);
}

vector<int> LgrLvqModel::GetPrototypeLabels() const {
    vector<int> retval(prototype.size());
    for (unsigned i = 0;i < prototype.size();++i)
        retval[i] = pLabel[i];
    return retval;
}

void LgrLvqModel::DoOptionalNormalization() {
    if (!settings.neiP)
        NormalizeRelevances();
}


Matrix_NN LgrLvqModel::PrototypeDistances(Matrix_NN const& points) const {
    Matrix_NN tmpPointsDiff(points.rows(), points.cols());

    Matrix_NN newPoints(prototype.size(), points.cols());
    for (size_t protoI = 0;protoI < prototype.size();++protoI) {
        tmpPointsDiff.noalias() = prototype[protoI].R.asDiagonal() * (points.colwise() - prototype[protoI].pos);
        newPoints.row(protoI).noalias() = tmpPointsDiff.colwise().squaredNorm();
    }
    return newPoints;
}

Matrix_NN LgrLvqModel::GetCombinedTransforms() const {
    Vector_N sum(prototype[0].R);

    Vector_N meanRelevances = std::accumulate(prototype.cbegin(), prototype.cend(), Vector_N(Vector_N::Zero(Dimensions())), [](Vector_N val, Prototype const& p) { return val + p.R; })
        / double(prototype.size());
    return meanRelevances.asDiagonal();
}
