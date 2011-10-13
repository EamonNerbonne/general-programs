#pragma once
#include "LvqProjectionModelBase.h"

class GmLvqModel : public LvqProjectionModelBase<GmLvqModel>
{
	//Matrix_P P; //in base class
	std::vector<Vector_N> prototype;
	std::vector<Vector_2, Eigen::aligned_allocator<Vector_2> > P_prototype;
	VectorXi pLabel;
	double totalMuJLr,totalMuKLr;

	std::vector<CorrectAndWorstMatches::MatchOk> ngMatchCache;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	Vector_N m_vJ, m_vK; //vectors of dimension DIMS

	EIGEN_STRONG_INLINE void RecomputeProjection(size_t protoIndex) {
		P_prototype[protoIndex].noalias() = P * prototype[protoIndex];
	}

protected:
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const;

public:
	virtual Matrix_NN PrototypeDistances(Matrix_NN const & points) const;

	//for templates:
	
	static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::GmModelType;
	inline int PrototypeLabel(int protoIndex) const {return pLabel(protoIndex);}
	inline int PrototypeCount() const {return static_cast<int>(pLabel.size());}
	inline double SqrDistanceTo(int protoIndex, Vector_2 const & P_otherPoint) const {
		return (P_prototype[protoIndex] - P_otherPoint).squaredNorm();
	}

//end for templates
	virtual size_t MemAllocEstimate() const;

	GmLvqModel(LvqModelSettings & initSettings);
	virtual int classify(Vector_N const & unknownPoint) const {return classifyProjectedInline(P * unknownPoint);}
	virtual int classifyProjected(Vector_2 const & unknownProjectedPoint) const {return classifyProjectedInline(unknownProjectedPoint);}
	EIGEN_STRONG_INLINE int classifyProjectedInline(Vector_2 const & P_otherPoint) const{
		double distance(std::numeric_limits<double>::infinity());
		int match(-1);

		for(int i=0;i<pLabel.size();i++) {
			double curDist = SqrDistanceTo(i, P_otherPoint);
			if(curDist < distance) { match=i; distance = curDist; }
		}
		assert( match >= 0 );
		return this->pLabel(match);
	}
	
	MatchQuality learnFrom(Vector_N const & newPoint, int classLabel);
	LvqModel* clone() const; 

	Matrix_2N GetProjectedPrototypes() const;
	std::vector<int> GetPrototypeLabels() const;
	virtual void DoOptionalNormalization();
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const;
	
	virtual void CopyTo(LvqModel& target) const{ 
		GmLvqModel & typedTarget = dynamic_cast<GmLvqModel&>(target);
		typedTarget = *this;
	}
};

