#pragma once
#include "stdafx.h"
#include "LvqProjectionModel.h"
#include "LvqModelFindMatches.h"

template<typename TDerivedModel> class LvqProjectionModelBase : public LvqProjectionModel, public LvqModelFindMatches<TDerivedModel, Vector2d> {
protected:
	LvqProjectionModelBase(boost::mt19937 & rngIter,int input_dims, int classCount) :LvqProjectionModel(rngIter,input_dims,classCount){}
public:
	void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const {
		TDerivedModel const & self = static_cast<TDerivedModel const &>(*this);
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

	GoodBadMatch ComputeMatches(VectorXd const & unknownPoint, int pointLabel) const { return this->findMatches(P * unknownPoint, pointLabel); }
};