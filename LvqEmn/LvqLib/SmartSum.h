#pragma once


using namespace Eigen;

template<typename T>
class SmartSum {
	

	T meanX, sX;
	double weightSum;
public:
	SmartSum(T const & zero) : meanX(zero), sX(zero), weightSum(0.0) {}

	void CombineWith(T const & val, double weight) {
		if(weight == 0.0) return;//ignore zero-weight stuff...
		double newWeightSum = weightSum + weight;
		double mScale = weight / newWeightSum;
		double sScale = weightSum * weight / newWeightSum;
		weightSum = newWeightSum;
		sX += (val - meanX)*(val - meanX) * sScale;
		meanX += (val - meanX) * mScale;
	}
	//void CombineWith(T const val, double weight) {
	//	CombineWith(val,weight);
	//}


	void CombineWithSum(SmartSum const & other) {
		if(weight == 0.0) return;//ignore zero-weight stuff...
		double newWeightSum = weightSum + other.weightSum;
		double mScale = other.weightSum / newWeightSum;
		double sScale = weightSum * other.weightSum / newWeightSum;
		weightSum = newWeightSum;
		sX += other.sX +  (other.meanX - meanX)*(other.meanX - meanX) * sScale;
		meanX += (val - meanX) * mScale;
	}

	T const & GetMean()const {return meanX;}
	T GetVariance() const {return sX / weightSum;}
	T GetSampleVariance() const {return sX / (weightSum-1);}
	double GetWeight() const {return weightSum;}
};

