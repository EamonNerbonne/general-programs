#pragma once
#include "stdafx.h"
#include "utils.h"
USING_PART_OF_NAMESPACE_EIGEN

#pragma intrinsic(pow)

template<class DerivedModel>
class AbstractLvqModel
{
	virtual void ignore(){}
	int trainIter;
protected:
	double iterationScaleFactor;
	inline double stepLearningRate() {
		double scaledIter = trainIter*iterationScaleFactor + 1.0;
		++trainIter;
		return 0.5/ sqrt(scaledIter*sqrt(scaledIter)); //*exp(-0.65*log(scaledIter));  
	}

public:
	AbstractLvqModel() : trainIter(0), iterationScaleFactor(0.01){ }

	inline int classify(VectorXd const & unknownPoint) const {return static_cast<DerivedModel const*>(this)->classifyImpl(unknownPoint); }
	inline void learnFrom(VectorXd const & newPoint, int classLabel)  {static_cast<DerivedModel*>(this)->learnFromImpl(newPoint,classLabel); }
	
	AbstractLvqModel<DerivedModel> * clone() const {return new DerivelModel(static_cast<DerivedModel const&>(*this)  );}
	inline size_t MemAllocEstimate() const {return static_cast<DerivedModel const*>(this)->MemAllocEstimateImpl(); }
};

template<class DerivedModel>
class AbstractProjectionLvqModel : public AbstractLvqModel<DerivedModel> {
protected:
	AbstractProjectionLvqModel(int input_dims) : P(LVQ_LOW_DIM_SPACE, input_dims)  {	P.setIdentity(); }
	PMatrix P;
public:
	PMatrix const & projectionMatrix() const {return P;}
	double projectionNorm() const { return projectionSquareNorm(P);  }
	void normalizeProjection() { normalizeMatrix(P); }
	void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const;
	inline int classifyProjected(Vector2d const & unknownProjectedPoint) const {return static_cast<DerivedModel const*>(this)->classifyProjectedImpl(unknownProjectedPoint); }

	inline int classifyImpl(VectorXd const & unknownPoint) const{
		using namespace std;
#if EIGEN3
		Vector2d P_unknownPoint;//TODO:test if this really is faster.
		P_unknownPoint.noalias() = P * unknownPoint;
#else
		Vector2d P_unknownPoint = (P * unknownPoint).lazy();
#endif
		return classifyProjected(P_unknownPoint);
	}

};

template<class DerivedModel>
inline void AbstractProjectionLvqModel<DerivedModel>::ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const {
	int cols = classDiagram.cols();
	int rows = classDiagram.rows();
	for(int xCol=0;  xCol < cols;  xCol++) {
		double x = x0 + (x1-x0) * (xCol+0.5) / cols;
		for(int yRow=0;  yRow < rows;  yRow++) {
			double y = y0+(y1-y0) * (yRow+0.5) / rows;
			Vector2d vec(x,y);
			classDiagram(yRow, xCol) = classifyProjected(vec);
		}
	}
}
