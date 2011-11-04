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
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, LvqDataset const * testSet) const;
public:
	typedef Eigen::Matrix<unsigned char,Eigen::Dynamic,Eigen::Dynamic,Eigen::RowMajor> ClassDiagramT;
	virtual ~LvqProjectionModel() { }
	Matrix_P const & projectionMatrix() const {return P;}
	virtual Matrix_NN GetCombinedTransforms() const {return P;}
	virtual int Dimensions() const {return static_cast<int>(P.cols());}
	virtual Matrix_2N GetProjectedPrototypes() const=0;
	virtual int classifyProjected(Vector_2 const & unknownProjectedPoint) const=0;
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const=0;
};

//#pragma managed(pop)
