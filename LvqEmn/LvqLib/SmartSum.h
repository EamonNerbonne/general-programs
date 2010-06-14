#pragma once
#include "stdafx.h"
using namespace Eigen;
template<typename T>
class SmartSum {
	typedef double TScalar;
	T meanX, sX;
	TScalar weightSum;
public:
	SmartSum(T const & zero) : meanX(zero), sX(zero), weightSum(static_cast<TScalar>(0.0)) {}

	template<typename S>
	void CombineWith(S const & val, typename TScalar weight) {
		if(weight == static_cast<TScalar>(0.0)) return;//ignore zero-weight stuff...
		TScalar newWeightSum = weightSum + weight;
		TScalar mScale = weight / newWeightSum;
		TScalar sScale = weightSum * weight / newWeightSum;
		weightSum = newWeightSum;
		sX.array() += (val.array() - meanX.array()).square() * sScale;
		meanX += (val - meanX) * mScale;
		
	}

	void CombineWithSum(SmartSum const & other) {
		if(weight == static_cast<TScalar>(0.0)) return;//ignore zero-weight stuff...
		TScalar newWeightSum = weightSum + other.weightSum;
		TScalar mScale = other.weightSum / newWeightSum;
		TScalar sScale = weightSum * other.weightSum / newWeightSum;
		weightSum = newWeightSum;
		sX.array() += other.sX +  (other.meanX.array() - meanX.array()).square() * sScale;
		meanX += (val - meanX) * mScale;
	}

	T const & GetMean()const {return meanX;}
	T GetVariance()const {return sX / weightSum;}
	T GetSampleVariance()const {return sX / (weightSum-1);}
	TScalar GetWeight() const {return weightSum;}
};

