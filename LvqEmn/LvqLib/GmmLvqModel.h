#pragma once
#include "LvqProjectionModelBase.h"
#include "GmmLvqPrototype.h"

using namespace Eigen;

class GmmLvqModel : public LvqProjectionModelBase<GmmLvqModel>
{
	typedef std::vector<GmmLvqPrototype, Eigen::aligned_allocator<GmmLvqPrototype> > protoList;

	protoList prototype;
	std::vector<CorrectAndWorstMatches::MatchOk> ngMatchCache;

	//we will preallocate a few temp vectors to reduce malloc/free overhead.
	VectorXd m_vJ, m_vK;
	PMatrix m_PpseudoinvT;
protected:
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const;


public:
	static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::GmmModelType;
		//for templates:

	inline int PrototypeLabel(int protoIndex) const {return prototype[protoIndex].label();}
	inline int PrototypeCount() const {return static_cast<int>(prototype.size());}
	inline double SqrDistanceTo(int protoIndex, Vector2d const & P_otherPoint) const { return prototype[protoIndex].SqrDistanceTo(P_otherPoint); }

//end for templates


	GmmLvqModel(LvqModelSettings & initSettings);
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

	MatchQuality ComputeMatches(VectorXd const & unknownPoint, int pointLabel) const { return this->findMatches(P * unknownPoint, pointLabel).GmmQuality(); }
	MatchQuality learnFrom(VectorXd const & newPoint, int classLabel);
	virtual LvqModel* clone() const ;

	virtual MatrixXd GetProjectedPrototypes() const;
	virtual std::vector<int> GetPrototypeLabels() const;
	virtual void DoOptionalNormalization();
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const;
	virtual void CopyTo(LvqModel& target) const{ 
		GmmLvqModel & typedTarget = dynamic_cast<GmmLvqModel&>(target);
		typedTarget = *this;
	}
};
