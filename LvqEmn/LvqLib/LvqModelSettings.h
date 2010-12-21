#pragma once
#include <vector>
#include <boost/random/mersenne_twister.hpp>
#include <Eigen/Core>
#include <boost/shared_ptr.hpp>
#include "LvqConstants.h"
#include "LvqTypedefs.h"

template <typename T> class copy_ptr {
	T*  item;
public:
	
	explicit copy_ptr(T const& existingItem) : item(new T(existingItem)) {}
	copy_ptr(copy_ptr<T> const & other) : item(new T(*other.item)) {}
	copy_ptr(copy_ptr<T> && tmpother) : item(tmpother.item) {tmpother.item=0;}

	~copy_ptr()  { delete item;}

	T const * get() const {return item;}
	T const * operator->() const {return get();}
	T const & operator*() const {return *item;}
	T  * get()  {return item;}
	T  * operator->()  {return get();}
	T & operator*() {return *item;}

	copy_ptr& operator=(copy_ptr const& rhs) { *item = *rhs.item; return *this; }
	  //copy_ptr tmp(rhs);
	  //std::swap(item, tmp.item);
	//  return *this;
	//}
	copy_ptr& operator=(copy_ptr && tmprhs) {
		item=tmprhs.item;
		tmprhs.item=0;
	    return *this;
	}
};

template <typename T> class copy_val {
	T  item;
public:
	
	explicit copy_val(T const& existingItem) : item(existingItem) {}
	copy_val(copy_val<T> const & other) : item(other.item) {}
	copy_val(copy_val<T> && tmpother) : item(std::move(tmpother.item)) {}
	~copy_val()  { }

	T const * get() const {return &item;}
	T const * operator->() const {return get();}
	T const & operator*() const {return item;}
	T  * get()  {return &item;}
	T  * operator->()  {return get();}
	T  & operator*()  {return item;}

	copy_val& operator=(copy_val const& rhs) { item = rhs.item; return *this; }
	copy_val& operator=(copy_val && tmprhs) { item = std::move(tmprhs.item); return *this; }
};

class LvqModelRuntimeSettings
{
public:
	bool TrackProjectionQuality,NormalizeProjection,NormalizeBoundaries,GloballyNormalize,UpdatePointsWithoutB;
	int ClassCount;
	double LrScaleP, LrScaleB, LR0,LrScaleBad;
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
		, RngIter(rngIter) { }
};

class LvqModelSettings
{
public:
	bool RandomInitialProjection;
	bool RandomInitialBorders;
	bool NgUpdateProtos;
	int Dimensionality;

	enum LvqModelType { AutoModelType, GmModelType, GsmModelType, G2mModelType, GmmModelType };
	LvqModelType ModelType;
	boost::mt19937 RngParams;
	LvqModelRuntimeSettings RuntimeSettings;
	std::vector<int> PrototypeDistribution;
	Eigen::MatrixXd PerClassMeans;
	PMatrix pcaTransform;
	LvqModelSettings(LvqModelType modelType, boost::mt19937 & rngParams, boost::mt19937 & rngIter, std::vector<int> protodistrib, Eigen::MatrixXd const & means, PMatrix const &pcaTransform) 
		: RandomInitialProjection(true)
		, RandomInitialBorders(false) 
		, NgUpdateProtos(false)
		, RngParams(rngParams)
		, PrototypeDistribution(protodistrib)
		, PerClassMeans(means)
		, ModelType(modelType)
		, RuntimeSettings(static_cast<int>(means.cols()),rngIter)
		, Dimensionality(0)
		, pcaTransform(pcaTransform)
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
