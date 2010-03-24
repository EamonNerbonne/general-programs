#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"
#include "G2mLvqPrototype.h"
#include "G2mLvqMatch.h"

USING_PART_OF_NAMESPACE_EIGEN

class G2mLvqPrototype;
class G2mLvqModel : public AbstractProjectionLvqModel
{
	std::vector<G2mLvqPrototype, Eigen::aligned_allocator<G2mLvqPrototype> > prototype;
	double lr_scale_P, lr_scale_B;
	const int classCount;

	//we will preallocate a few temp vectors to reduce malloc/free overhead.
	VectorXd m_vJ, m_vK;

	inline int classifyProjectedInternal(Vector2d const & P_unknownPoint) const {
		G2mLvqMatch matches(&P_unknownPoint);
		for(int i=0;i<prototype.size(); ++ i)	matches.AccumulateMatch(prototype[i]);
		assert(matches.match != NULL);
		return matches.match->ClassLabel();
	}

	inline int classifyInternal(VectorXd const & unknownPoint) const { return classifyProjectedInternal(P * unknownPoint); }
public:

	G2mLvqModel(boost::mt19937 & rng, bool randInit, std::vector<int> protodistribution, MatrixXd const & means);
	virtual size_t MemAllocEstimate() const;
	int classify(VectorXd const & unknownPoint) const {return classifyInternal(unknownPoint);}
	int classifyProjected(Vector2d const & unknownProjectedPoint) const { return classifyProjectedInternal(unknownProjectedPoint);}
	void learnFrom(VectorXd const & newPoint, int classLabel);
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const;
	virtual AbstractLvqModel* clone();
};


