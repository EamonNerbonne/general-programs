#pragma once
#include <vector>
#include <boost/random/mersenne_twister.hpp>
#include <Eigen/Core>
#include <boost/scoped_ptr.hpp>

class LvqModelRuntimeSettings
{
public:
	bool TrackProjectionQuality;
	int ClassCount;
	boost::scoped_ptr<boost::mt19937> RngIter;
	LvqModelRuntimeSettings(int classCount, boost::mt19937 & rngIter) 
		: TrackProjectionQuality(false)
		, ClassCount(classCount)
		, RngIter(new boost::mt19937(rngIter)) { }
	LvqModelRuntimeSettings(LvqModelRuntimeSettings const & toCopy) 
		: TrackProjectionQuality(toCopy.TrackProjectionQuality)
		, ClassCount(toCopy.ClassCount)
		, RngIter(new boost::mt19937(*toCopy.RngIter)) { }
};

class LvqModelSettings
{
public:
	bool RandomInitialProjection;
	bool RandomInitialBorders;

	enum LvqModelType {	 AutoModelType, GmModelType, GsmModelType, G2mModelType };
	LvqModelType ModelType;
	boost::mt19937 RngParams;
	LvqModelRuntimeSettings RuntimeSettings;
	std::vector<int> PrototypeDistribution;
	Eigen::MatrixXd PerClassMeans;
	LvqModelSettings(LvqModelType modelType, boost::mt19937 & rngParams, boost::mt19937 & rngIter, std::vector<int> protodistrib, Eigen::MatrixXd const & means) 
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
