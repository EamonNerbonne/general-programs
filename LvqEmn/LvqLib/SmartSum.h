#pragma once
#include <Eigen/Core>
using namespace Eigen;

template<int CompileTimeDims>
class SmartSum {
	typedef Eigen::Array<double,CompileTimeDims,1> ArrayT;
	ArrayT meanX, sX;
	double weightSum;
public:
	SmartSum(int dims) : meanX(ArrayT::Zero(dims) ), sX(ArrayT::Zero(dims)), weightSum(0.0) {}

	void CombineWith(double val, double weight) {
		assert(CompileTimeDims == 1);
		ArrayT valA(1);
		valA(0,0) = val;
		CombineWith(valA, weight);
	}

	void CombineWith(ArrayT const & val, double weight) {
		if(weight == 0.0) return;//ignore zero-weight stuff...
		double newWeightSum = weightSum + weight;
		double mScale = weight / newWeightSum;
		double sScale = weightSum * weight / newWeightSum;
		weightSum = newWeightSum;
		sX += (val - meanX) *(val - meanX) * sScale;
		meanX += (val - meanX) * mScale;
	}

	void CombineWithSum(SmartSum const & other) {
		if(weight == 0.0) return;//ignore zero-weight stuff...
		double newWeightSum = weightSum + other.weightSum;
		double mScale = other.weightSum / newWeightSum;
		double sScale = weightSum * other.weightSum / newWeightSum;
		weightSum = newWeightSum;
		sX += other.sX +  (other.meanX - meanX)*(other.meanX - meanX) * sScale;
		meanX += (val - meanX) * mScale;
	}

	ArrayT const & GetMean()const {return meanX;}
	ArrayT GetVariance() const {return sX / weightSum;}
	ArrayT GetSampleVariance() const {return sX / (weightSum-1);}
	double GetWeight() const {return weightSum;}
	void Reset() { 
		meanX  = ArrayT::Zero(meanX.size());
		sX = ArrayT::Zero(sX.size());
		weightSum = 0.0;
	}
};

