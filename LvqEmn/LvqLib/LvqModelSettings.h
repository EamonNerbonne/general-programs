#pragma once
#include <vector>
#include <boost/random/mersenne_twister.hpp>
#include <Eigen/Core>
class LvqModelRuntimeSettings
{
public:
	bool TrackProjectionQuality;
	int ClassCount;
	boost::mt19937 RngIter;
	LvqModelRuntimeSettings(int classCount, boost::mt19937 & rngIter) 
		: TrackProjectionQuality(false)
		, ClassCount(classCount)
		, RngIter(rngIter) { }
};

class LvqModelSettings
{
public:
	enum LvqModelType {
		AutoModelType, GmModelType, GsmModelType, G2mModelType
	};
	bool RandomInitialProjection;
	bool RandomInitialBorders;
	boost::mt19937 RngParams;
	std::vector<int> PrototypeDistribution;
	Eigen::MatrixXd PerClassMeans;
	LvqModelType ModelType;
	LvqModelRuntimeSettings RuntimeSettings;
	LvqModelSettings(LvqModelType modelType, boost::mt19937 & rngParams, boost::mt19937 & rngIter, std::vector<int> protodistrib, MatrixXd const & means) 
		: RandomInitialProjection(true)
		, RandomInitialBorders(false) 
		, RngParams(rngParams)
		, PrototypeDistribution(protodistrib)
		, PerClassMeans(means)
		, ModelType(modelType)
		, RuntimeSettings(static_cast<int>(means.cols()),rngIter)
	{ }

	size_t Dimensions() const {return PerClassMeans.rows();}

	template<typename T>
	void AssertModelIsOfRightType(T * model) {
		if(ModelType == AutoModelType)
			ModelType = T::ThisModelType;
		else if(ModelType != T::ThisModelType)
			throw "Invalid Model Type!";
	}

	LvqModelSettings& self(){return *this;}
};

class LvqModel;

LvqModel* ConstructLvqModel(LvqModelSettings & initSettings);
