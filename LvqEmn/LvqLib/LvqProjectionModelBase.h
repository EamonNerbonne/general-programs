#pragma once
#include "LvqProjectionModel.h"
#include "LvqModelFindMatches.h"

template<typename TDerivedModel> class LvqProjectionModelBase : public LvqProjectionModel, public LvqModelFindMatches<TDerivedModel, Vector2d> {
protected:
	LvqProjectionModelBase(LvqModelSettings & initSettings) : LvqProjectionModel(initSettings) { }
public:
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const {
		TDerivedModel const & self = static_cast<TDerivedModel const &>(*this);
		int cols = static_cast<int>(classDiagram.cols());
		int rows = static_cast<int>(classDiagram.rows());
		double xDelta = (x1-x0) / cols;
		double yDelta = (y1-y0) / rows;
		double xBase = x0+xDelta*0.5;
		double yBase = y0+yDelta*0.5;

		double y = yBase;
		for(int yRow=0;  yRow < rows;  yRow++) {
			double x = xBase;
			for(int xCol=0;  xCol < cols;  xCol++) {
				classDiagram(yRow,xCol) = self.classifyProjectedInline(Vector2d(x,y));
				x+=xDelta;
			}
			y+=yDelta;
		}
	}

	GoodBadMatch ComputeMatches(VectorXd const & unknownPoint, int pointLabel) const { return this->findMatches(P * unknownPoint, pointLabel); }
};