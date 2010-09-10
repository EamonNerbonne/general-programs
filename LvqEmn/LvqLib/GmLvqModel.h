#pragma once
#include "stdafx.h"
#include "LvqModel.h"
#include "LvqModelFindMatches.h"

using boost::scoped_array;
using std::vector;
class GmLvqModel : public LvqModel, public LvqModelFindMatches<GmLvqModel,VectorXd>
{
	vector<MatrixXd > P; //<double,Eigen::Dynamic,Eigen::Dynamic,Eigen::ColMajor,Eigen::Dynamic,Eigen::Dynamic>
	vector<VectorXd> prototype;
	VectorXi pLabel;
	double lr_scale_P;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK;
	mutable VectorXd tmpHelper1, tmpHelper2; //vectors of dimension DIMS

protected:
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset) const;


public:
//for templates:
	inline int PrototypeLabel(int protoIndex) const {return pLabel(protoIndex);}
	inline int PrototypeCount() const {return static_cast<int>(pLabel.size());}

	EIGEN_STRONG_INLINE double SqrDistanceTo(int protoIndex, VectorXd const & otherPoint) const {
#if EIGEN3
		tmpHelper1.noalias() = prototype[protoIndex] - otherPoint;
		tmpHelper2.noalias() = P[protoIndex] * tmpHelper1;
		return tmpHelper2.squaredNorm();
#else
		tmpHelper1 = prototype[protoIndex] - otherPoint;
		tmpHelper2 = (P[protoIndex] * tmpHelper1).lazy();
		return tmpHelper2.squaredNorm();
#endif
	}
	//end for templates




	virtual size_t MemAllocEstimate() const;
	virtual int Dimensions() const {return static_cast<int>(P[0].cols());}
	virtual double meanProjectionNorm() const;

	GmLvqModel(boost::mt19937 & rngParams, boost::mt19937 & rngIter,  bool randInit, std::vector<int> protodistribution, MatrixXd const & means);
	virtual GoodBadMatch ComputeMatches(VectorXd const & unknownPoint, int pointLabel) const;
	virtual int classify(VectorXd const & unknownPoint) const; 
	virtual GoodBadMatch learnFrom(VectorXd const & newPoint, int classLabel);
	virtual GmLvqModel* clone() const { return new GmLvqModel(*this); }
};

inline int GmLvqModel::classify(VectorXd const & unknownPoint) const{
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
