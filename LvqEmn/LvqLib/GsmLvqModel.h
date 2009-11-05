#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"

class GsmLvqModel : public AbstractProjectionLvqModel
{
	PMatrix P;
	//boost::scoped_array<VectorXd> prototype;
	std::vector<VectorXd> prototype;
	std::vector<Vector2d, Eigen::aligned_allocator<Vector2d>  > P_prototype;
	VectorXi pLabel;
	double lr_scale_P;
	const int classCount;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK, dQdwJ, dQdwK, tmpHelper; //vectors of dimension DIMS
	PMatrix dQdP;

	inline double SqrDistanceTo(int protoIndex, Vector2d const & P_otherPoint) const {
		return (P_prototype[protoIndex] - P_otherPoint).squaredNorm();
	}
	
	struct GoodBadMatch {
		double distGood, distBad;
		int matchGood, matchBad;
		GoodBadMatch()
			: distGood(std::numeric_limits<double>::infinity())
			, distBad(std::numeric_limits<double>::infinity())
			, matchGood(-1)
			, matchBad(-1)
		{}
	};
	GoodBadMatch findMatches(Vector2d const & P_trainPoint, int trainLabel);

	inline void RecomputeProjection(int protoIndex);
	inline int classifyProjectedInternal(Vector2d const & unknownProjectedPoint) const;

public:
	virtual double iterationScaleFactor() const {return 1.0/pLabel.size();}
	virtual double projectionNorm() const { return (P.transpose() * P).lazy().diagonal().sum() ;}

	PMatrix const & getProjection() const {return P; }

	GsmLvqModel(std::vector<int> protodistribution, MatrixXd const & means);
	int classify(VectorXd const & unknownPoint) const;
	int classifyProjected(Vector2d const & unknownProjectedPoint) const{return classifyProjectedInternal(unknownProjectedPoint);}

	void learnFrom(VectorXd const & newPoint, int classLabel);
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const;
	virtual AbstractLvqModel* clone() {
		return new GsmLvqModel(*this);
	}
};

