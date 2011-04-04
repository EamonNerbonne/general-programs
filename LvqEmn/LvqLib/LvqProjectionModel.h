#pragma once
//#pragma managed(push, off)

#include <Eigen/Core>

#include "LvqModel.h"
#include "LvqTypedefs.h"

class LvqProjectionModel : public LvqModel {
protected:
	Matrix_P P;

	LvqProjectionModel(LvqModelSettings & initSettings);
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const;
public:
	typedef Eigen::Matrix<unsigned char,Eigen::Dynamic,Eigen::Dynamic,Eigen::RowMajor> ClassDiagramT;
	virtual ~LvqProjectionModel() { }
	Matrix_P const & projectionMatrix() const {return P;}
	virtual int Dimensions() const {return static_cast<int>(P.cols());}
	virtual Matrix_NN GetProjectedPrototypes() const=0;
	virtual int classifyProjected(Vector_2 const & unknownProjectedPoint) const=0;
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const=0;
};

//#pragma managed(pop)
