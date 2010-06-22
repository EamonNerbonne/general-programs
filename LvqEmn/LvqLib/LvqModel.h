#pragma once
#include "stdafx.h"
#include "utils.h"
#include "LvqTrainingStat.h"
#include "LvqConstants.h"
#include "GoodBadMatch.h"
#pragma intrinsic(pow)

class LvqDataset;
class LvqModel
{
	unsigned long long trainIter;
	unsigned long long totalIter;
	double totalElapsed;
	boost::mt19937 rngIter;

protected:
	double iterationScaleFactor;
	inline double stepLearningRate() {
		double scaledIter = trainIter*iterationScaleFactor + 1.0;
		++trainIter;
		return LVQ_LR0 /sqrt(scaledIter*sqrt(scaledIter)); // significantly faster than exp(-0.75*log(scaledIter)) due to fewer cache misses;  
	}

	const int classCount;
public:
	boost::mt19937 & RngIter() {return rngIter;}
	std::vector<LvqTrainingStat> trainingStats;
	void resetLearningRate() {trainIter=0;}

	LvqModel(boost::mt19937 & rngIter,int classCount) : trainIter(0), totalIter(0), totalElapsed(0.0), rngIter(rngIter), iterationScaleFactor(LVQ_PERCLASSITERFACTOR/classCount),classCount(classCount){ }
	void AddTrainingStat(double trainingMeanCost,double trainingErrorRate, LvqDataset const * testSet,  std::vector<int>const & testSubset, int iterInc, double elapsedInc);
	void AddTrainingStat(LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset, int iterInc, double elapsedInc);
	int ClassCount() const { return classCount; }

	virtual int classify(VectorXd const & unknownPoint) const=0; 
	virtual void computeCostAndError(VectorXd const & unknownPoint, int pointLabel,bool&err,double&cost) const=0;
	virtual double meanProjectionNorm() const=0; 
	virtual VectorXd otherStats() const { return VectorXd::Zero((int)LvqTrainingStats::Extra); }
	virtual void learnFrom(VectorXd const & newPoint, int classLabel, bool *wasError, double* hadCost)=0;
	virtual ~LvqModel() {	}
	virtual LvqModel* clone() const=0;
	virtual size_t MemAllocEstimate() const=0;
	virtual int Dimensions() const =0;
};




template<typename TDerivedModel, typename TProcessedPoint> class AbstractLvqModelBase {
protected:
#pragma warning (disable: 4127)
#define ASSTRING(X) #X
#define DBG(X) (cout<<ASSTRING(X)<<": "<<X<<"\n")

	EIGEN_STRONG_INLINE GoodBadMatch findMatches(TProcessedPoint const & trainPoint, int trainLabel) const {
		using std::cout;
		GoodBadMatch match;
		TDerivedModel const & self = static_cast<TDerivedModel const &>(*this);

		for(int i=0;i<self.PrototypeCount();i++) {
			double curDist = self.SqrDistanceTo(i, trainPoint);
			if(self.PrototypeLabel(i) == trainLabel) {
				if(curDist < match.distGood) {
					match.matchGood = i;
					match.distGood = curDist;
				}
			} else {
				if(curDist < match.distBad) {
					match.matchBad = i;
					match.distBad = curDist;
				}
			}
		}

		if(match.matchBad < 0 ||match.matchGood <0) {
			assert( match.matchBad >= 0 && match.matchGood >=0 );
			DBG(match.matchBad);
			DBG(match.matchGood);
			DBG(match.distBad);
			DBG(match.distGood);
			DBG(self.PrototypeCount());//WTF: this statement impacts gcc _correctness_?
		}
		return match;
	}

};