#pragma once
#include <vector>
#include <boost/random/mersenne_twister.hpp>
#include <Eigen/Core>
#include <boost/shared_ptr.hpp>
#include "LvqConstants.h"
#include "LvqTypedefs.h"
#include "copy_ptr.h"

class LvqModelRuntimeSettings
{
public:
	bool TrackProjectionQuality,NormalizeProjection,NormalizeBoundaries,GloballyNormalize,UpdatePointsWithoutB,SlowStartLrBad;
	int ClassCount;
	double LrScaleP, LrScaleB, LR0,LrScaleBad;
	copy_ptr<boost::mt19937> RngIter;
	LvqModelRuntimeSettings(int classCount, boost::mt19937 & rngIter) 
		: TrackProjectionQuality(false)
		, NormalizeProjection(true)
		, NormalizeBoundaries(false)
		, UpdatePointsWithoutB(false)
		, GloballyNormalize(true)
		, SlowStartLrBad(false)
		, ClassCount(classCount)
		, LrScaleP(LVQ_LrScaleP)
		, LrScaleB(LVQ_LrScaleB)
		, LR0(LVQ_LR0)
		, LrScaleBad(LVQ_LrScaleBad)
		, RngIter(rngIter) { }
};

class LvqDataset;

class LvqModelSettings
{
public:
	bool RandomInitialProjection;
	bool RandomInitialBorders;
	bool NgUpdateProtos;
	bool NgInitializeProtos;
	int Dimensionality;

	enum LvqModelType { AutoModelType, LgmModelType, GmModelType, G2mModelType, GgmModelType };
	LvqModelType ModelType;
	boost::mt19937 RngParams;
	LvqModelRuntimeSettings RuntimeSettings;
	std::vector<int> PrototypeDistribution;
	LvqDataset const * Dataset;
	std::vector<int> Trainingset;

	size_t ClassCount() const { return PrototypeDistribution.size(); }
	std::pair<Eigen::MatrixXd,Eigen::VectorXi> InitByClassMeans() const;
	std::pair<Eigen::MatrixXd,Eigen::VectorXi> InitByNg() ;
	std::pair<Eigen::MatrixXd,Eigen::VectorXi> InitProtosBySetting() ;
	PMatrix pcaTransform() const;
	int PrototypeCount() const;

	LvqModelSettings(LvqModelType modelType, boost::mt19937 & rngParams, boost::mt19937 & rngIter, std::vector<int> protodistrib, LvqDataset const * dataset, std::vector<int> trainingset); 
	size_t Dimensions() const;

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
