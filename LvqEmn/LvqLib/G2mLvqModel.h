#pragma once
#include "LvqProjectionModelBase.h"
#include "G2mLvqPrototype.h"

using namespace Eigen;

class G2mLvqModel : public LvqProjectionModelBase<G2mLvqModel>
{
    typedef std::vector<G2mLvqPrototype, Eigen::aligned_allocator<G2mLvqPrototype> > protoList;
    double totalMuJLr, totalMuKLr;
    double lastAutoPupdate;
    protoList prototype;
    std::vector<CorrectAndWorstMatches::MatchOk> ngMatchCache;
    //std::vector<double> bScaleCache;

    //we will preallocate a few temp vectors to reduce malloc/free overhead.
    Vector_N m_vJ, m_vK;
    void NormalizeBoundaries();
protected:
    virtual void AppendTrainingStatNames(std::vector<std::wstring>& retval) const;
    virtual void AppendOtherStats(std::vector<double>& stats, LvqDataset const* trainingSet, LvqDataset const* testSet) const;

    virtual void compensateProjectionUpdate(Matrix_22 U, double scale);

public:
    virtual Matrix_NN PrototypeDistances(Matrix_NN const& points) const;


    static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::G2mModelType;
    //for templates:

    inline int PrototypeLabel(int protoIndex) const { return prototype[protoIndex].label(); }
    inline int PrototypeCount() const { return static_cast<int>(prototype.size()); }
    inline double SqrDistanceTo(int protoIndex, Vector_2 const& P_otherPoint) const { return prototype[protoIndex].SqrDistanceTo(P_otherPoint); }

    //end for templates


    G2mLvqModel(LvqModelSettings& initSettings);
    virtual size_t MemAllocEstimate() const;
    virtual int classify(Vector_N const& unknownPoint) const { return classifyProjectedInline(P * unknownPoint); }
    virtual int classifyProjected(Vector_2 const& unknownProjectedPoint) const { return classifyProjectedInline(unknownProjectedPoint); }
    EIGEN_STRONG_INLINE int classifyProjectedInline(Vector_2 const& P_unknownPoint) const {
        double distance(std::numeric_limits<double>::infinity());
        int match(-1);

        for (int i = 0;i<int(prototype.size());i++) {
            double curDist = prototype[i].SqrDistanceTo(P_unknownPoint);
            if (curDist < distance) { match = i; distance = curDist; }
        }
        assert(match >= 0);
        return prototype[match].classLabel;
    }

    MatchQuality learnFrom(Vector_N const& newPoint, int classLabel);
    virtual LvqModel* clone() const;

    virtual Matrix_2N GetProjectedPrototypes() const;
    virtual std::vector<int> GetPrototypeLabels() const;
    virtual void DoOptionalNormalization();

    virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT& classDiagram) const;
    virtual void CopyTo(LvqModel& target) const {
        G2mLvqModel& typedTarget = dynamic_cast<G2mLvqModel&>(target);
        typedTarget = *this;
    }
};
