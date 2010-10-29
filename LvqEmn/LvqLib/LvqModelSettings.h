#pragma once
#include <vector>
#include <boost/random/mersenne_twister.hpp>
#include <Eigen/Core>
#include <boost/scoped_ptr.hpp>
#include "LvqConstants.h"
template <typename T> class copy_ptr {
	T* item;
public:
	explicit copy_ptr(T* ptr) : item(ptr) {}
	copy_ptr(copy_ptr<T> const & other) : item(new T(*other.item)) {}
	~copy_ptr()  { delete item;}

	T  * get() const {return item;}
	T & operator*() const {return *item;}
	T * operator->() const {return item;}
};

class LvqModelRuntimeSettings
{
public:
	bool TrackProjectionQuality,NormalizeProjection,NormalizeBoundaries,GloballyNormalize,UpdatePointsWithoutB;
	int ClassCount;
	double  LrScaleP, LrScaleB, LR0,LrScaleBad;
	copy_ptr<boost::mt19937> RngIter;
	LvqModelRuntimeSettings(int classCount, boost::mt19937 & rngIter) 
		: TrackProjectionQuality(false)
		, NormalizeProjection(false)
		, NormalizeBoundaries(false)
		, UpdatePointsWithoutB(false)
		, GloballyNormalize(true)
		, ClassCount(classCount)
		, LrScaleP(LVQ_LrScaleP)
		, LrScaleB(LVQ_LrScaleB)
		, LR0(LVQ_LR0)
		, LrScaleBad(LVQ_LrScaleBad)
		, RngIter(new boost::mt19937(rngIter)) { }
};

class LvqModelSettings
{
public:
	bool RandomInitialProjection;
	bool RandomInitialBorders;
	bool NgUpdateProtos;
	int Dimensionality;

	enum LvqModelType {	 AutoModelType, GmModelType, GsmModelType, G2mModelType };
	LvqModelType ModelType;
	boost::mt19937 RngParams;
	LvqModelRuntimeSettings RuntimeSettings;
	std::vector<int> PrototypeDistribution;
	Eigen::MatrixXd PerClassMeans;
	LvqModelSettings(LvqModelType modelType, boost::mt19937 & rngParams, boost::mt19937 & rngIter, std::vector<int> protodistrib, Eigen::MatrixXd const & means) 
		: RandomInitialProjection(true)
		, RandomInitialBorders(false) 
		, NgUpdateProtos(false)
		, RngParams(rngParams)
		, PrototypeDistribution(protodistrib)
		, PerClassMeans(means)
		, ModelType(modelType)
		, RuntimeSettings(static_cast<int>(means.cols()),rngIter)
		, Dimensionality(0)
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
