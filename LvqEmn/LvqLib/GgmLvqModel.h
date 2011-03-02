#pragma once
#include "LvqProjectionModelBase.h"
#include "GgmLvqPrototype.h"

using namespace Eigen;

class GgmLvqModel : public LvqProjectionModelBase<GgmLvqModel>
{
	typedef std::vector<GgmLvqPrototype, Eigen::aligned_allocator<GgmLvqPrototype> > protoList;

	protoList prototype;
	std::vector<CorrectAndWorstMatches::MatchOk> ngMatchCache;

	//we will preallocate a few temp vectors to reduce malloc/free overhead.
	VectorXd m_vJ, m_vK;
	PMatrix m_PpseudoinvT;
protected:
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const;
	virtual bool IdenticalMu() const {return true;}


public:
	static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::GgmModelType;
		//for templates:

	inline int PrototypeLabel(size_t protoIndex) const {return prototype[protoIndex].label();}
	inline int PrototypeCount() const {return static_cast<int>(prototype.size());}
	inline double SqrDistanceTo(size_t protoIndex, Vector2d const & P_otherPoint) const { return prototype[protoIndex].SqrDistanceTo(P_otherPoint); }

//end for templates


	GgmLvqModel(LvqModelSettings & initSettings);
	virtual size_t MemAllocEstimate() const;
	virtual int classify(VectorXd const & unknownPoint) const {return classifyProjectedInline(P * unknownPoint);}
	virtual int classifyProjected(Vector2d const & unknownProjectedPoint) const { return classifyProjectedInline(unknownProjectedPoint);}
	EIGEN_STRONG_INLINE int classifyProjectedInline(Vector2d const & P_unknownPoint) const {
		double distance(std::numeric_limits<double>::infinity());
		int match(-1);

		for(int i=0;i<int(prototype.size());i++) {
			double curDist = prototype[i].SqrDistanceTo(P_unknownPoint);
			if(curDist < distance) { match=i; distance = curDist; }
		}
		assert( match >= 0 );
		return prototype[match].classLabel;
	}

	MatchQuality ComputeMatches(VectorXd const & unknownPoint, int pointLabel) const { return this->findMatches(P * unknownPoint, pointLabel).GgmQuality(); }
	MatchQuality learnFrom(VectorXd const & newPoint, int classLabel);
	virtual LvqModel* clone() const ;

	virtual MatrixXd GetProjectedPrototypes() const;
	virtual std::vector<int> GetPrototypeLabels() const;
	virtual void DoOptionalNormalization();
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const;
	virtual void CopyTo(LvqModel& target) const{ 
		GgmLvqModel & typedTarget = dynamic_cast<GgmLvqModel&>(target);
		typedTarget = *this;
	}
};
