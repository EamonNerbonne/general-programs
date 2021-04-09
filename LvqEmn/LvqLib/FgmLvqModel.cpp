#include "stdafx.h"
#include "FgmLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"
#include "MeanMinMax.h"
#include "LvqDataset.h"
#include "PCA.h"
#include "CovarianceAndMean.h"
#include "randomUnscalingMatrix.h"
using namespace std;
using namespace Eigen;

//this is like Ggm, except the cost function includes the probabilities of the point being not just in the nearest prototypes distribution, but of all points.


FgmLvqModel::FgmLvqModel(LvqModelSettings & initSettings)
    : LvqProjectionModelBase(initSettings)
    , totalMuJLr(0.0)
    , totalMuKLr(0.0)
    , lastAutoPupdate(0.0)
    , m_vJ(initSettings.InputDimensions())
    , m_vK(initSettings.InputDimensions())
    , m_Pdelta(LVQ_LOW_DIM_SPACE, initSettings.InputDimensions())
{
    if(initSettings.OutputDimensions() != 2) {
        std::cerr<< "Illegal Dimensionality\n";
        std::exit(10);
    }
    using namespace std;
    initSettings.AssertModelIsOfRightType(this);

    auto InitProtos = initSettings.InitProtosProjectionBoundariesBySetting();
    P = get<0>(InitProtos);
    normalizeProjection(P);
    Matrix_NN prototypes = get<1>(InitProtos);
    VectorXi protoLabels = get<2>(InitProtos);
    vector<Matrix_22, Eigen::aligned_allocator<Matrix_22>> initB = get<3>(InitProtos);

    prototype.resize(protoLabels.size());
    m_dists.resize(protoLabels.size());
    m_probs.resize(protoLabels.size());

    for(size_t protoIndex=0; protoIndex < (size_t)protoLabels.size(); ++protoIndex) {
        prototype[protoIndex].point.resize(initSettings.InputDimensions());
        prototype[protoIndex] =     FgmLvqPrototype(initSettings.RngParams, initSettings.RandomInitialBorders, protoLabels(protoIndex), prototypes.col(protoIndex), P,MakeUpperTriangular<Matrix_22>(initB[protoIndex]));
    }

    if(settings.scP) {
        Matrix_2N pPoints = initSettings.Dataset->ProjectPoints(*this);
        VectorXi const & label = initSettings.Dataset->getPointLabels();
        double sumLogScale=0.0;
        for(ptrdiff_t pI = 0; pI<label.size();++pI) {
            auto matches = findMatches(pPoints.col(pI), label(pI));
            double logScale = log((prototype[matches.matchGood].P_point -pPoints.col(pI)).squaredNorm() + (prototype[matches.matchBad].P_point -pPoints.col(pI)).squaredNorm());
            sumLogScale+=logScale;
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
        for(size_t i=0;i<prototype.size();++i)
            prototype[i].ComputePP(P);
    }
}

typedef Map<Vector_N, Aligned> MVectorXd;

MatchQuality FgmLvqModel::learnFrom(Vector_N const & trainPoint, int trainLabel) {
    using namespace std;
    const size_t protoCount = prototype.size();

    Vector_2 P_trainPoint( P * trainPoint );



    auto matchInfo = ComputeMatchesInternal(P_trainPoint, trainLabel);
    MatchQuality quality = get<0>(matchInfo);
    double probJsum = get<1>(matchInfo);
    double probKsum = get<2>(matchInfo);
    size_t bestJ = get<3>(matchInfo);

    double muJ = probKsum / sqr(probKsum+probJsum);
    double muK = -probJsum / sqr(probKsum+probJsum);

    double learningRate = stepLearningRate(bestJ);
    double lr_point = -settings.LR0 * learningRate,
        lr_P = lr_point * settings.LrScaleP,
        lr_B = lr_point * settings.LrScaleB,// * (1.0 - learningRate),
        lr_bad = (settings.SlowK  ?  sqr(1.0 - learningRate)  :  1.0) * settings.LrScaleBad;

    assert(lr_P<=0 && lr_B<=0 && lr_point<=0);
    m_Pdelta.setZero();


    double rawDistSum = 0.0;

    MVectorXd v(m_vJ.data(),m_vJ.size());
    for(size_t i=0;i<prototype.size(); ++ i) {
        FgmLvqPrototype &proto = prototype[i];
        double mu2,mu2_alt;

        if(proto.classLabel == trainLabel ) {
            mu2 = muJ * m_probs(i) * 2.0;//positive
            mu2_alt = mu2 + (settings.MuOffset != 0.0 ? settings.MuOffset * learningRate *  exp(-0.5*m_dists(i)) : 0);
            assert(mu2 >= 0.0);
            totalMuJLr -= lr_point * mu2;//compensate for lr_point negative
        }
        else {
            mu2 = muK * m_probs(i) * 2.0;//negative
            mu2_alt = mu2 * lr_bad; //only applies to B and point!
            assert(lr_point <=0.0 && mu2 <=0.0);
            totalMuKLr += lr_point * mu2; 
        }

        Vector_2 P_v= proto.P_point - P_trainPoint;
        Vector_2 B_P_v = proto.B.triangularView<Eigen::Upper>() * P_v;
        v = proto.point - trainPoint;
        Vector_2 BT_B_P_v =  proto.B.triangularView<Eigen::Upper>().transpose() * B_P_v ;

        //inverse of upper triangular matrix is upper trangular.
        //  - thus the inverses transpose is lower triangular
        //  - thus the upper triangular component of the transpose of the inverse is diagonal.

        //note: since A^{-1} A = I; we have trivially that the diagonal of the inverse is the coefficient-wise reciprocal of the diagonal

        //Matrix_22 BinvT =  proto.B.inverse().transpose().triangularView<Eigen::Upper>();//TODO:triangular
        Vector_2 BinvTdiag = proto.B.diagonal().cwiseInverse();

        if(settings.scP) {
            rawDistSum += (proto.P_point - P_trainPoint).squaredNorm();
            //if(settings.scP) lastAutoPupdate *= LVQ_AutoScaleP_Momentum;
            //            lastAutoPupdate -=  log( (proto.P_point - P_trainPoint).squaredNorm() + (prototype[matches.matchBad].P_point - P_trainPoint).squaredNorm());
        }


        proto.B.triangularView<Eigen::Upper>() += (lr_B * (mu2_alt)) * (B_P_v * P_v.transpose() );
        proto.B.diagonal() -= (lr_B * (mu2_alt)) * BinvTdiag;
        proto.RecomputeBias();

        proto.point.noalias() += P.transpose()* ((lr_point * (mu2_alt)) * BT_B_P_v);

        //if(settings.scP) P *= exp(lastAutoPupdate*4*learningRate*LVQ_AutoScaleP_Lr);


        if(!settings.noKP || proto.classLabel == trainLabel) {
            m_Pdelta.noalias() += ((lr_P * mu2) * BT_B_P_v) * v.transpose();
        }
    }
    P.noalias() += m_Pdelta;
    if(!settings.scP)
        normalizeProjection(P);

    for(size_t i=0; i < protoCount;++i)
        prototype[i].ComputePP(P);

    return quality;
}


LvqModel* FgmLvqModel::clone() const { return new FgmLvqModel(*this); }

size_t FgmLvqModel::MemAllocEstimate() const {
    return 
        sizeof(FgmLvqModel) +
        sizeof(double) * P.size() +
        sizeof(double) * (m_vJ.size() +m_vK.size() + m_probs.size() + m_dists.size()) + //various temps
        sizeof(FgmLvqPrototype)*prototype.size() + //prototypes; part statically allocated
        sizeof(double) * (prototype.size() * m_vJ.size()) + //prototypes; part dynamically allocated
        (16/2) * (8+prototype.size()*2);//estimate for alignment mucking.
}

Matrix_2N FgmLvqModel::GetProjectedPrototypes() const {
    Matrix_2N retval(LVQ_LOW_DIM_SPACE, static_cast<int>(prototype.size()));
    for(unsigned i=0;i<prototype.size();++i)
        retval.col(i) = prototype[i].projectedPosition();
    return retval;
}

vector<int> FgmLvqModel::GetPrototypeLabels() const {
    vector<int> retval(prototype.size());
    for(unsigned i=0;i<prototype.size();++i)
        retval[i] = prototype[i].label();
    return retval;
}

void FgmLvqModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const {
    LvqProjectionModel::AppendTrainingStatNames(retval);
    retval.push_back(L"Maximum norm(B)!norm!Border Matrix norm");
    retval.push_back(L"Mean norm(B)!norm!Border Matrix norm");
    retval.push_back(L"Minimum norm(B)!norm!Border Matrix norm");
    retval.push_back(L"Maximum |B|!determinant!Border Matrix absolute determinant");
    retval.push_back(L"Mean |B|!determinant!Border Matrix absolute determinant");
    retval.push_back(L"Minimum |B|!determinant!Border Matrix absolute determinant");
    retval.push_back(L"Prototype bias max!bias!Prototype bias");
    retval.push_back(L"Prototype bias mean!bias!Prototype bias");
    retval.push_back(L"Prototype bias min!bias!Prototype bias");
    retval.push_back(L"Cumulative \u03BC-scaled Learning Rate!!Cumulative \u03BC-scaled Learning Rates");
}
void FgmLvqModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, LvqDataset const * testSet) const {
    LvqProjectionModel::AppendOtherStats(stats,trainingSet,testSet);
    MeanMinMax norm, det, bias;
    std::for_each(prototype.begin(),prototype.end(), [&](FgmLvqPrototype const & proto) {
        norm.Add(proto.B(1,0));
        det.Add(abs(proto.B.triangularView<Eigen::Upper>().determinant()));
        bias.Add(proto.bias);
    });

    stats.push_back(norm.max());
    stats.push_back(norm.mean());
    stats.push_back(norm.min());

    stats.push_back(det.max());
    stats.push_back(det.mean());
    stats.push_back(det.min());

    stats.push_back(bias.max());
    stats.push_back(bias.mean());
    stats.push_back(bias.min());
    stats.push_back(totalMuJLr);
}

void FgmLvqModel::ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const {
    int cols = static_cast<int>(classDiagram.cols());
    int rows = static_cast<int>(classDiagram.rows());
    double xDelta = (x1-x0) / cols;
    double yDelta = (y1-y0) / rows;
    double xBase = x0+xDelta*0.5;
    double yBase = y0+yDelta*0.5;

    Matrix_P B_diff_x0_y(LVQ_LOW_DIM_SPACE,PrototypeCount()); //Contains B_i * (testPoint[x, y0] - P*proto_i)  for all proto's i
    //will update to include changes to X.
    Matrix_P B_xDelta(LVQ_LOW_DIM_SPACE,PrototypeCount());//Contains B_i * (xDelta, 0)  for all proto's i
    Matrix_P B_yDelta(LVQ_LOW_DIM_SPACE,PrototypeCount());//Contains B_i * (0 , yDelta)  for all proto's i
    Vector_N pBias(PrototypeCount());//Contains prototype[i].bias for all proto's i

    for(int pi=0; pi < this->PrototypeCount(); ++pi) {
        auto & current_proto = this->prototype[pi];
        B_diff_x0_y.col(pi).noalias() = current_proto.B.triangularView<Eigen::Upper>() * ( Vector_2(xBase,yBase) - current_proto.P_point);
        B_xDelta.col(pi).noalias() = current_proto.B.triangularView<Eigen::Upper>() * Vector_2(xDelta,0.0);
        B_yDelta.col(pi).noalias() = current_proto.B.triangularView<Eigen::Upper>() * Vector_2(0.0,yDelta);
        pBias(pi) = current_proto.bias;
    }

    for(int yRow=0; yRow < rows; yRow++) {
        Matrix_P B_diff_x_y(B_diff_x0_y); //copy that includes changes to X as well.
        for(int xCol=0; xCol < cols; xCol++) {
            // x = xBase + xCol * xDelta;  y = yBase + yCol * yDelta;
            Matrix_NN::Index bestProtoI;
            (B_diff_x_y.colwise().squaredNorm() + pBias.transpose()).minCoeff(&bestProtoI);
            classDiagram(yRow, xCol) =(unsigned char) this->prototype[bestProtoI].classLabel;

            B_diff_x_y.noalias() += B_xDelta;
        }
        B_diff_x0_y.noalias() += B_yDelta;
    }
}

void FgmLvqModel::DoOptionalNormalization() {
    //THIS IS JUST BAD for GGM; we normalize each iter.
}

void FgmLvqModel::compensateProjectionUpdate(Matrix_22 U, double /*scale*/) {
    for(size_t i=0;i < prototype.size();++i) {
        prototype[i].B.noalias() = MakeUpperTriangular<Matrix_22>(prototype[i].B * U);
        prototype[i].ComputePP(P);
    }
}

FgmLvqPrototype::FgmLvqPrototype() :  B(Matrix_22::Zero()), classLabel(-1) {}

FgmLvqPrototype::FgmLvqPrototype(std::mt19937 & rng, bool randInit, int protoLabel, Vector_N const & initialVal,Matrix_P const & P, Matrix_22 const & scaleB) 
    : B(scaleB)//randInit?randomUnscalingMatrix<Matrix_22>(rng, LVQ_LOW_DIM_SPACE)*scaleB: 
    , P_point(P*initialVal)
    , classLabel(protoLabel)
    , point(initialVal) 
    , bias(0.0)
{
    auto rndmat = randomUnscalingMatrix<Matrix_22>(rng, LVQ_LOW_DIM_SPACE);
    if(randInit)
        B = MakeUpperTriangular<Matrix_22>(rndmat*B);
    RecomputeBias();
}


Matrix_NN FgmLvqModel::PrototypeDistances(Matrix_NN const & points) const {
    Matrix_2N P_points = P*points;
    Matrix_NN newPoints(prototype.size(), points.cols());
    for(size_t protoI=0;protoI<prototype.size();++protoI) {
        newPoints.row(protoI).noalias() = (prototype[protoI].B.triangularView<Eigen::Upper>() * (P_points.colwise() - prototype[protoI].P_point)).colwise().squaredNorm();
    }
    return newPoints;
}