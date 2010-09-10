#pragma once
#include "stdafx.h"
#include "LvqConstants.h"
#include "utils.h"
#include "LvqModel.h"


class LvqProjectionModel : public LvqModel {
protected:
	LvqProjectionModel(boost::mt19937 & rngIter,int input_dims, int classCount) :LvqModel(rngIter,classCount), P(LVQ_LOW_DIM_SPACE, input_dims)  {	P.setIdentity(); }
	PMatrix P;

	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset) const;

public:
	virtual ~LvqProjectionModel() { }
	PMatrix const & projectionMatrix() const {return P;}
	double projectionNorm() const { return projectionSquareNorm(P);  }
	void normalizeProjection() { normalizeMatrix(P); }
	virtual double meanProjectionNorm() const {return projectionNorm();}
	virtual int Dimensions() const {return static_cast<int>(P.cols());}

	virtual MatrixXd GetProjectedPrototypes() const=0;
	virtual std::vector<int> GetPrototypeLabels() const=0;
	virtual int classifyProjected(Vector2d const & unknownProjectedPoint) const=0;
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const=0;

};

