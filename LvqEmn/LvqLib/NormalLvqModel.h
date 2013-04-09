#pragma once
#include "LvqProjectionModelBase.h"
#include "GgmLvqPrototype.h"
#include <Eigen/Core>
#include "LvqTypedefs.h"
#include "utils.h"

using namespace Eigen;
using std::vector;

class NormalLvqPrototype
{
	friend class NormalLvqModel;
	Matrix_NN P;
	Vector_N P_point;

	int classLabel; //only set during initialization.
	Vector_N point;
	double bias;//-ln(det(B)^2)

	EIGEN_STRONG_INLINE void RecomputeBias() {
		bias = - log(sqr(P.diagonal().prod())); //B.diagonal().prod() == B.determinant() due to upper triangular B.
		assert(isfinite_emn(bias));
	}

public:
	inline int label() const {return classLabel;}
	inline Matrix_NN const & matP() const {return P;}
	inline Vector_N const & position() const{return point;}
	inline Vector_N const & projectedPosition() const{return P_point;}

	NormalLvqPrototype();

	NormalLvqPrototype(boost::mt19937 & rng, bool randInit, int protoLabel, Vector_N const & initialVal, Matrix_NN const & P);

	inline double SqrDistanceTo(Vector_N const & testPoint, Vector_N & tmp) const {
		tmp.noalias() = P * testPoint - P_point;
		return tmp.squaredNorm() + bias;//waslazy
	}

	//EIGEN_MAKE_ALIGNED_OPERATOR_NEW
};

class NormalLvqModel : public LvqModel, public LvqModelFindMatches<NormalLvqModel, Vector_N>
{
	vector<Matrix_NN > P; 
	vector<Vector_N> prototype;
	VectorXi pLabel;

	typedef std::vector<NormalLvqPrototype> protoList;

	double totalMuLr,lastAutoPupdate;
	protoList prototype;
	std::vector<CorrectAndWorstMatches::MatchOk> ngMatchCache;

	//we will preallocate a few temp vectors to reduce malloc/free overhead.
	//Vector_N m_vJ, m_vK;
	//Matrix_P m_PpseudoinvT;
	mutable Vector_N tmp_dist;
protected:
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, LvqDataset const * testSet) const;
	virtual bool IdenticalMu() const {return true;}



public:
	virtual Matrix_NN PrototypeDistances(Matrix_NN const & points) const;

	static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::NormalModelType;
	//for templates:

	inline int PrototypeLabel(size_t protoIndex) const {return prototype[protoIndex].label();}
	inline int PrototypeCount() const {return static_cast<int>(prototype.size());}
	inline double SqrDistanceTo(size_t protoIndex, Vector_N const & otherPoint) const { return prototype[protoIndex].SqrDistanceTo(otherPoint, tmp_dist); }

	//end for templates


	NormalLvqModel(LvqModelSettings & initSettings);
	virtual size_t MemAllocEstimate() const;
	virtual int classify(Vector_N const & unknownPoint) const {
		double distance(std::numeric_limits<double>::infinity());
		int match(-1);

		for(int i=0;i<int(prototype.size());i++) {
			double curDist = SqrDistanceTo(i, unknownPoint);
			if(curDist < distance) { match=i; distance = curDist; }
		}
		assert( match >= 0 );
		return prototype[match].classLabel;
	}

	MatchQuality ComputeMatches(Vector_N const & unknownPoint, int pointLabel) const { return this->findMatches(unknownPoint, pointLabel).GgmQuality(); }
	MatchQuality learnFrom(Vector_N const & newPoint, int classLabel);
	virtual LvqModel* clone() const ;

	virtual std::vector<int> GetPrototypeLabels() const;
	virtual void DoOptionalNormalization();
	virtual void CopyTo(LvqModel& target) const{ 
		NormalLvqModel & typedTarget = dynamic_cast<NormalLvqModel&>(target);
		typedTarget = *this;
	}
};
