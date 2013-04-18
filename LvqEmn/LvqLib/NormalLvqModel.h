#pragma once
#include "LvqProjectionModelBase.h"
#include "GgmLvqPrototype.h"
#include <Eigen/Core>
#include "LvqTypedefs.h"
#include "utils.h"

using namespace Eigen;
using std::vector;

//class NormalLvqPrototype
//{
//	friend class NormalLvqModel;
//	Matrix_NN P;
//	Vector_N P_point;
//
//	int classLabel; //only set during initialization.
//	Vector_N point;
//	double bias;//-ln(det(B)^2)
//
//	EIGEN_STRONG_INLINE void RecomputeBias() {
//		bias = - log(sqr(P.diagonal().prod())); //B.diagonal().prod() == B.determinant() due to upper triangular B.
//		assert(isfinite_emn(bias));
//	}
//
//public:
//	inline int label() const {return classLabel;}
//	inline Matrix_NN const & matP() const {return P;}
//	inline Vector_N const & position() const{return point;}
//	inline Vector_N const & projectedPosition() const{return P_point;}
//
//	NormalLvqPrototype();
//
//	NormalLvqPrototype(boost::mt19937 & rng, bool randInit, int protoLabel, Vector_N const & initialVal, Matrix_NN const & P);
//
//	inline double SqrDistanceTo(Vector_N const & testPoint, Vector_N & tmp) const {
//		tmp.noalias() = P * testPoint - P_point;
//		return tmp.squaredNorm() + bias;//waslazy
//	}
//
//	//EIGEN_MAKE_ALIGNED_OPERATOR_NEW
//};

class NormalLvqModel : public LvqModel, public LvqModelFindMatches<NormalLvqModel, Vector_N>
{
	vector<Matrix_NN > P; //DIMSOUT rows; DIMS cols; protocount matrices.
	vector<Vector_N> prototype;
	VectorXi pLabel;
	Vector_N pBias;
	double totalMuJLr,totalMuKLr;
	double sumUpdateSize;
	mutable double lastSumUpdateSize;

	unsigned updatesOverOne;
	mutable unsigned lastUpdatesOverOne;

	mutable double lastStatIter;

	//calls dimensionality of input-space DIMS, output space DIMSOUT
	//we will preallocate a few vectors to reduce malloc/free overhead.

	mutable Vector_N tmpSrcDimsV1, tmpSrcDimsV2,tmpSrcDimsV3,tmpSrcDimsV4; //vectors of dimension DIMS
	mutable Vector_N tmpDestDimsV1,tmpDestDimsV2,tmpDestDimsV3,tmpDestDimsV4; //vector of dimension DIMSOUT

	
protected:
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, LvqDataset const * testSet) const;
	virtual bool IdenticalMu() const {return true;}

public:
	virtual Matrix_NN PrototypeDistances(Matrix_NN const & points) const;
	virtual Matrix_NN GetCombinedTransforms() const;


	static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::NormalModelType;
	//for templates:

	inline int PrototypeLabel(size_t protoIndex) const {return pLabel(protoIndex);}
	inline int PrototypeCount() const {return static_cast<int>(prototype.size());}
	
	EIGEN_STRONG_INLINE double SqrDistanceTo(size_t protoIndex, Vector_N const & otherPoint) const { 
		tmpSrcDimsV1.noalias() = otherPoint - prototype[protoIndex];
		tmpDestDimsV1.noalias() = P[protoIndex] * tmpSrcDimsV1;
		return (tmpDestDimsV1).squaredNorm() + pBias[protoIndex];
	}

	//end for templates
	virtual size_t MemAllocEstimate() const;
	virtual int Dimensions() const {return static_cast<int>(P[0].cols());}

	NormalLvqModel(LvqModelSettings & initSettings);
	MatchQuality ComputeMatches(Vector_N const & unknownPoint, int pointLabel) const { return this->findMatches(unknownPoint, pointLabel).GgmQuality(); }
	virtual void DoOptionalNormalization();
	virtual std::vector<int> GetPrototypeLabels() const;
	virtual int classify(Vector_N const & unknownPoint) const {
		double distance(std::numeric_limits<double>::infinity());
		int match(-1);

		for(int i=0;i<int(prototype.size());i++) {
			double curDist = SqrDistanceTo(i, unknownPoint);
			if(curDist < distance) { match=i; distance = curDist; }
		}
		assert( match >= 0 );
		return pLabel(match);
	}

	MatchQuality learnFrom(Vector_N const & newPoint, int classLabel);
	virtual NormalLvqModel* clone() const { return new NormalLvqModel(*this); }

	virtual void CopyTo(LvqModel& target) const{ 
		NormalLvqModel & typedTarget = dynamic_cast<NormalLvqModel&>(target);
		typedTarget = *this;
	}
};
