#pragma once
#include <Eigen/Core>

#include "LvqModel.h"
#include "LvqTypedefs.h"

class LvqProjectionModel : public LvqModel {
protected:
	PMatrix P;

	LvqProjectionModel(LvqModelSettings & initSettings);
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset) const;
public:
	virtual ~LvqProjectionModel() { }
	PMatrix const & projectionMatrix() const {return P;}
	virtual int Dimensions() const {return static_cast<int>(P.cols());}
	virtual MatrixXd GetProjectedPrototypes() const=0;
	virtual std::vector<int> GetPrototypeLabels() const=0;
	virtual int classifyProjected(Vector2d const & unknownProjectedPoint) const=0;
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const=0;
};

