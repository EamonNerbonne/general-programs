#pragma once
#include "LvqModel.h"
#include "LvqModelFindMatches.h"
#include <boost/scoped_array.hpp>

using boost::scoped_array;
using std::vector;
class LgmLvqModel : public LvqModel, public LvqModelFindMatches<LgmLvqModel,VectorXd>
{
	vector<MatrixXd > P; 
	vector<VectorXd> prototype;
	VectorXi pLabel;

	//calls dimensionality of input-space DIMS, output space DIMSOUT
	//we will preallocate a few vectors to reduce malloc/free overhead.

	mutable VectorXd tmpSrcDimsV1, tmpSrcDimsV2; //vectors of dimension DIMS
	mutable VectorXd tmpDestDimsV1,tmpDestDimsV2; //vector of dimension DIMSOUT

protected:
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const;


public:
	static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::LgmModelType;
	inline int PrototypeLabel(int protoIndex) const {return pLabel(protoIndex);}
	inline int PrototypeCount() const {return static_cast<int>(pLabel.size());}

	EIGEN_STRONG_INLINE double SqrDistanceTo(int protoIndex, VectorXd const & otherPoint) const {
		tmpSrcDimsV1.noalias() = prototype[protoIndex] - otherPoint;
		tmpDestDimsV1.noalias() = P[protoIndex] * tmpSrcDimsV1;
		return tmpDestDimsV1.squaredNorm();
	}

	virtual size_t MemAllocEstimate() const;
	virtual int Dimensions() const {return static_cast<int>(P[0].cols());}

	LgmLvqModel(LvqModelSettings & initSettings);
	virtual MatchQuality ComputeMatches(VectorXd const & unknownPoint, int pointLabel) const;
	virtual void DoOptionalNormalization();
	virtual std::vector<int> GetPrototypeLabels() const;
	virtual int classify(VectorXd const & unknownPoint) const; 
	virtual MatchQuality learnFrom(VectorXd const & newPoint, int classLabel);
	virtual LgmLvqModel* clone() const { return new LgmLvqModel(*this); }
	virtual void CopyTo(LvqModel& target) const{ 
		LgmLvqModel & typedTarget = dynamic_cast<LgmLvqModel&>(target);
		typedTarget = *this;
	}
};

inline int LgmLvqModel::classify(VectorXd const & unknownPoint) const{
	using namespace std;
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	for(int i=0;i<pLabel.size();++i) {
		double curDist = SqrDistanceTo(i, unknownPoint);
		if(curDist < distance) {
			match=i;
			distance = curDist;
		}
	}
	assert( match >= 0 );
	return this->pLabel(match);
}