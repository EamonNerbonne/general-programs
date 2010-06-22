#pragma once
#include "stdafx.h"
#include "LvqConstants.h"
#include "utils.h"
#include "LvqModel.h"
#include "LvqTrainingStat.h"


class LvqProjectionModel : public LvqModel {
protected:
	LvqProjectionModel(boost::mt19937 & rngIter,int input_dims, int classCount) :LvqModel(rngIter,classCount), P(LVQ_LOW_DIM_SPACE, input_dims)  {	P.setIdentity(); }
	PMatrix P;
	template <typename T>
	static void ClassBoundaryDiagramImpl(T const &self, double x0, double x1, double y0, double y1, MatrixXi & classDiagram) {
		int cols = static_cast<int>(classDiagram.cols());
		int rows = static_cast<int>(classDiagram.rows());
		for(int xCol=0;  xCol < cols;  xCol++) {
			double x = x0 + (x1-x0) * (xCol+0.5) / cols;
			for(int yRow=0;  yRow < rows;  yRow++) {
				double y = y0+(y1-y0) * (yRow+0.5) / rows;
				Vector2d vec(x,y);
				classDiagram(yRow,xCol) = self.classifyProjectedInline(vec);
			}
		}
	}

public:
	virtual ~LvqProjectionModel() { }
	PMatrix const & projectionMatrix() const {return P;}
	double projectionNorm() const { return projectionSquareNorm(P);  }
	virtual double meanProjectionNorm() const {return projectionNorm();}
	void normalizeProjection() { normalizeMatrix(P); }
	virtual int Dimensions() const {return static_cast<int>(P.cols());}

	virtual MatrixXd GetProjectedPrototypes() const=0;
	virtual std::vector<int> GetPrototypeLabels() const=0;
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const=0;
};

