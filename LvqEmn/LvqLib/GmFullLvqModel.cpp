#include "StdAfx.h"
#include "GmFullLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"
#include "LvqDataset.h"
using namespace std;

GmFullLvqModel::GmFullLvqModel(LvqModelSettings& initSettings)
    : LvqModel(initSettings)
    , totalMuJLr(0.0)
    , totalMuKLr(0.0)
    , lastAutoPupdate(0.0)
    , m_vJ(initSettings.InputDimensions())
    , m_vK(initSettings.InputDimensions())
    , m_vTmp1(size_t(initSettings.OutputDimensions()))
    , m_vTmp2(size_t(initSettings.OutputDimensions()))
    , m_vTmp3(size_t(initSettings.OutputDimensions()))
{
    initSettings.AssertModelIsOfRightType(this);

    auto InitProtos = initSettings.InitProtosAndProjectionBySetting();
    auto prototypes = get<1>(InitProtos);
    pLabel.resizeLike(get<2>(InitProtos));
    pLabel = get<2>(InitProtos);
    size_t protoCount = pLabel.size();
    P.resizeLike(get<0>(InitProtos));
    P = get<0>(InitProtos);
    normalizeProjection(P);

    prototype.resize(protoCount);
    P_prototype.resize(protoCount);

    for (size_t protoIndex = 0; protoIndex < protoCount; ++protoIndex) {
        prototype[protoIndex].resizeLike(prototypes.col(protoIndex));
        prototype[protoIndex] = prototypes.col(protoIndex);
        P_prototype[protoIndex].resize(P.rows());
        RecomputeProjection(protoIndex);
    }

    int maxProtoCount = accumulate(initSettings.PrototypeDistribution.begin(), initSettings.PrototypeDistribution.end(), 0, [](int a, int b) -> int { return max(a, b); });

    if (initSettings.NGu && maxProtoCount > 1)
        ngMatchCache.resize(maxProtoCount);//otherwise size 0!
    DoOptionalNormalization();

    if (settings.scP) {
        Matrix_NN pPoints = P * initSettings.Dataset->getPoints();
        Vector_N alignedMapHack = pPoints.col(0);
        auto alignedMap(Vector_N::MapAligned(alignedMapHack.data(), alignedMapHack.size()));
        VectorXi const& label = initSettings.Dataset->getPointLabels();
        double sumLogScale = 0.0;
        for (ptrdiff_t pI = 0; pI < label.size();++pI) {
            alignedMap.noalias() = pPoints.col(pI);
            auto matches = findMatches(alignedMap, label(pI));
            double logScale = log(matches.distGood + matches.distBad);
            sumLogScale += logScale;
        }
        double meanLogScale = sumLogScale / label.size();
        /*
        if not zero, we need to "subtract" this mean out from each match
        so E(ln(dJ+dk)) -= mean
        so each ln(dJ+dk) -= mean
        so each dJ+dK /= exp(mean)
        so each dJ+dK *= exp(-mean)
        so P^2*[...] *= exp(-mean);
        so P *= exp(-mean/2)

        */
        //
        lastAutoPupdate = -0.5 * meanLogScale;
        P *= exp(lastAutoPupdate);
        for (int i = 0;i < pLabel.size();++i)
            RecomputeProjection(i);
    }
}

MatchQuality GmFullLvqModel::learnFrom(Vector_N const& trainPoint, int trainLabel) {
    auto P_trainPoint(Vector_N::MapAligned(m_vTmp1.data(), m_vTmp1.size()));
    P_trainPoint.noalias() = P * trainPoint;

    CorrectAndWorstMatches fullmatch(0);
    GoodBadMatch matches;
    if (ngMatchCache.size() > 0) {
        fullmatch = CorrectAndWorstMatches(&(ngMatchCache[0]));
        for (int i = 0;i < (int)prototype.size();++i)
            fullmatch.Register(SqrDistanceTo(i, P_trainPoint), i, PrototypeLabel(i) == trainLabel);
        fullmatch.SortOk();
        matches = fullmatch.ToGoodBadMatch();
    }
    else {
        matches = findMatches(P_trainPoint, trainLabel);
    }

    double learningRate = stepLearningRate(matches.matchGood);
    double lr_point = settings.LR0 * learningRate,
        lr_P = lr_point * settings.LrScaleP,
        lr_bad = (settings.SlowK ? sqr(1.0 - learningRate) : 1.0) * settings.LrScaleBad;
    assert(lr_P >= 0 && lr_point >= 0);


    //now matches.good is "J" and matches.bad is "K".
    MatchQuality retval = matches.LvqQuality();
    double muK2 = 2.0 * retval.muK;
    double muJ2 = 2.0 * retval.muJ;

    int J = matches.matchGood;
    int K = matches.matchBad;

    auto muJ2_P_vJ(Vector_N::MapAligned(m_vTmp2.data(), m_vTmp2.size()));
    auto muK2_P_vK(Vector_N::MapAligned(m_vTmp3.data(), m_vTmp3.size()));

    muJ2_P_vJ.noalias() = muJ2 * (P_prototype[J] - P_trainPoint);
    muK2_P_vK.noalias() = muK2 * (P_prototype[K] - P_trainPoint);

    auto vJ(Vector_N::MapAligned(m_vJ.data(), m_vJ.size()));
    auto vK(Vector_N::MapAligned(m_vK.data(), m_vK.size()));

    vJ = prototype[K] - trainPoint;
    vK = prototype[K] - trainPoint;

    prototype[J].noalias() -= P.transpose() * (lr_point * muJ2_P_vJ);
    prototype[K].noalias() -= P.transpose() * (lr_bad * lr_point * muK2_P_vK);

    if (ngMatchCache.size() > 0) {
        double lrSub = lr_point;
        double lrDelta = exp(-LVQ_NG_FACTOR / learningRate);//this is rather ad hoc
        for (int i = 1;i < fullmatch.foundOk;++i) {
            lrSub *= lrDelta;
            Vector_N& Js = prototype[fullmatch.matchesOk[i].idx];
            Vector_N& P_Js = P_prototype[fullmatch.matchesOk[i].idx];
            double muJ2s_lrSub = lrSub * 2.0 * +2.0 * fullmatch.distBad / (sqr(fullmatch.matchesOk[i].dist) + sqr(fullmatch.distBad));
            Js.noalias() -= P.transpose() * (muJ2s_lrSub * (P_Js - P_trainPoint));
        }
    }

    if (settings.scP) {
        //double scale = -log(matches.distBad+matches.matchGood)*4*learningRate;
        //P *= exp(scale);P *= 1+x;
        lastAutoPupdate = LVQ_AutoScaleP_Momentum * lastAutoPupdate - log(matches.distBad + matches.distGood);
        double thisupdate = lastAutoPupdate * 4 * learningRate * LVQ_AutoScaleP_Lr;

        P *= exp(thisupdate);
    }


    P.noalias() -= (lr_P * muJ2_P_vJ) * vJ.transpose();
    if (!settings.noKP)
        P.noalias() -= (lr_P * muK2_P_vK) * vK.transpose();

    if (!settings.scP && (settings.neiP || settings.noKP))
        normalizeProjection(P);

    for (int i = 0;i < pLabel.size();++i)
        RecomputeProjection(i);

    totalMuJLr += lr_point * retval.muJ;
    totalMuKLr -= lr_point * retval.muK;

    return retval;
}

LvqModel* GmFullLvqModel::clone() const { return new GmFullLvqModel(*this); }

vector<int> GmFullLvqModel::GetPrototypeLabels() const {
    vector<int> retval(prototype.size());
    for (unsigned i = 0;i < prototype.size();++i)
        retval[i] = pLabel[i];
    return retval;
}

size_t GmFullLvqModel::MemAllocEstimate() const {
    return
        sizeof(GmFullLvqModel) + //base structure size
        sizeof(int) * pLabel.size() + //dyn.alloc labels
        sizeof(double) * (P.size()) + //dyn alloc transform + temp transform
        sizeof(double) * (m_vJ.size() + m_vK.size() + m_vTmp1.size() + m_vTmp2.size() + m_vTmp3.size()) + //various vector temps
        sizeof(Vector_N) * prototype.size() +//dyn alloc prototype base overhead
        sizeof(Vector_N) * P_prototype.size() + //cache of pretransformed prototypes
        sizeof(double) * (prototype.size() * (prototype[0].size() + P_prototype[0].size())) + //dyn alloc prototype data + dyn alloc pretransformed data
        (16 / 2) * (5 + prototype.size() * 2);//estimate for alignment mucking.
}


void GmFullLvqModel::DoOptionalNormalization() {
    if (!settings.scP && !settings.neiP && !settings.noKP) {
        normalizeProjection(P);
        for (size_t i = 0;i < prototype.size();++i)
            RecomputeProjection((int)i);
    }
}


void GmFullLvqModel::AppendTrainingStatNames(std::vector<std::wstring>& retval) const {
    LvqModel::AppendTrainingStatNames(retval);
    retval.push_back(L"Cumulative \u03BC-J-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
    retval.push_back(L"Cumulative \u03BC-K-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
}

void GmFullLvqModel::AppendOtherStats(std::vector<double>& stats, LvqDataset const* trainingSet, LvqDataset const* testSet) const {
    LvqModel::AppendOtherStats(stats, trainingSet, testSet);
    stats.push_back(totalMuJLr);
    stats.push_back(totalMuKLr);
}


Matrix_NN GmFullLvqModel::PrototypeDistances(Matrix_NN const& points) const {
    Matrix_2N P_points = P * points;
    Matrix_NN newPoints(prototype.size(), points.cols());
    for (size_t protoI = 0;protoI < prototype.size();++protoI) {
        newPoints.row(protoI).noalias() = (P_points.colwise() - P_prototype[protoI]).colwise().squaredNorm();
    }
    return newPoints;
}

Matrix_NN GmFullLvqModel::GetCombinedTransforms() const {
    return P;
}
