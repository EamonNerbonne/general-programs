#pragma once
#include "LvqProjectionModelBase.h"

class GmFullLvqModel : public LvqModel, public LvqModelFindMatches<GmFullLvqModel,Vector_N::AlignedMapType>
{
	Matrix_NN P;
	std::vector<Vector_N> prototype;
	std::vector<Vector_N> P_prototype;
	VectorXi pLabel;
	double totalMuJLr,totalMuKLr, lastAutoPupdate;

	std::vector<CorrectAndWorstMatches::MatchOk> ngMatchCache;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	Vector_N m_vJ, m_vK; //vectors of dimension DIMS
	mutable Vector_N m_vTmp1, m_vTmp2,m_vTmp3; //vector of internal DIMS

	EIGEN_STRONG_INLINE void RecomputeProjection(size_t protoIndex) {
		P_prototype[protoIndex].noalias() = P * prototype[protoIndex];
	}

protected:
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, LvqDataset const * testSet) const;

public:
	virtual Matrix_NN PrototypeDistances(Matrix_NN const & points) const;
	virtual Matrix_NN GetCombinedTransforms() const;

	//for templates:
	
	static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::GmModelType;
	inline int PrototypeLabel(int protoIndex) const {return pLabel(protoIndex);}
	inline int PrototypeCount() const {return static_cast<int>(pLabel.size());}
	inline double SqrDistanceTo(int protoIndex, Vector_N::AlignedMapType const & P_otherPoint) const {
		return (P_prototype[protoIndex] - P_otherPoint).squaredNorm();
	}
	inline double SqrDistanceTo(int protoIndex, Vector_N const & P_otherPoint) const {
		return (P_prototype[protoIndex] - P_otherPoint).squaredNorm();
	}

//end for templates
	virtual size_t MemAllocEstimate() const;

	GmFullLvqModel(LvqModelSettings & initSettings);
	virtual int classify(Vector_N const & unknownPoint) const {return classifyProjectedInline(P * unknownPoint);}
//	virtual int classifyProjected(Vector_N const & unknownProjectedPoint) const {return classifyProjectedInline(unknownProjectedPoint);}
	EIGEN_STRONG_INLINE int classifyProjectedInline(Vector_N const & P_otherPoint) const{
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

	std::vector<int> GetPrototypeLabels() const;
	virtual MatchQuality ComputeMatches(Vector_N const & unknownPoint, int pointLabel) const {
		auto P_point(Vector_N::MapAligned(m_vTmp1.data(),m_vTmp1.size()));
		P_point.noalias() = P * unknownPoint;
		return this->findMatches(P_point, pointLabel).LvqQuality(); 
	}
	virtual int Dimensions() const {return static_cast<int>(P.cols());}
	virtual void DoOptionalNormalization();
	
	virtual void CopyTo(LvqModel& target) const{ 
		GmFullLvqModel & typedTarget = dynamic_cast<GmFullLvqModel&>(target);
		typedTarget = *this;
	}
};

