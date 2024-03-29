#pragma once
#include "LvqModel.h"
#include "LvqModelFindMatches.h"

using std::vector;
using std::pair;

class LgrLvqModel : public LvqModel, public LvqModelFindMatches<LgrLvqModel, Vector_N>
{
    struct Prototype {
        Vector_N R, pos;
    };

    vector<Prototype> prototype;
    VectorXi pLabel;
    double totalMuJLr, totalMuKLr;
    //calls dimensionality of input-space DIMS, output space DIMSOUT
    //we will preallocate a few vectors to reduce malloc/free overhead.

    //mutable Vector_N tmpSrcDimsV1, tmpSrcDimsV2; //vectors of dimension DIMS
    mutable Vector_N tmpV1, tmpV2, tmpV3, tmpV4; //vector of dimension DIMSOUT

    inline void NormalizeRelevances();


protected:
    virtual void AppendTrainingStatNames(std::vector<std::wstring>& retval) const;
    virtual void AppendOtherStats(std::vector<double>& stats, LvqDataset const* trainingSet, LvqDataset const* testSet) const;


public:
    virtual Matrix_NN PrototypeDistances(Matrix_NN const& points) const;
    virtual Matrix_NN GetCombinedTransforms() const;


    static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::LgrModelType;
    inline int PrototypeLabel(int protoIndex) const { return pLabel(protoIndex); }
    inline int PrototypeCount() const { return static_cast<int>(pLabel.size()); }

    EIGEN_STRONG_INLINE double SqrDistanceTo(int protoIndex, Vector_N const& otherPoint) const {
        auto const& proto = prototype[protoIndex];
        return (proto.R.asDiagonal() * (proto.pos - otherPoint)).squaredNorm();
    }

    virtual size_t MemAllocEstimate() const;
    virtual int Dimensions() const { return static_cast<int>(prototype[0].pos.rows()); }

    LgrLvqModel(LvqModelSettings& initSettings);
    virtual MatchQuality ComputeMatches(Vector_N const& unknownPoint, int pointLabel) const;
    virtual void DoOptionalNormalization();
    virtual std::vector<int> GetPrototypeLabels() const;
    virtual int classify(Vector_N const& unknownPoint) const;
    virtual MatchQuality learnFrom(Vector_N const& newPoint, int classLabel);
    virtual LgrLvqModel* clone() const { return new LgrLvqModel(*this); }
    virtual void CopyTo(LvqModel& target) const {
        LgrLvqModel& typedTarget = dynamic_cast<LgrLvqModel&>(target);
        typedTarget = *this;
    }
};

inline int LgrLvqModel::classify(Vector_N const& unknownPoint) const {
    using namespace std;
    double distance(std::numeric_limits<double>::infinity());
    int match(-1);

    for (int i = 0;i < pLabel.size();++i) {
        double curDist = SqrDistanceTo(i, unknownPoint);
        if (curDist < distance) {
            match = i;
            distance = curDist;
        }
    }
    assert(match >= 0);
    return this->pLabel(match);
}
