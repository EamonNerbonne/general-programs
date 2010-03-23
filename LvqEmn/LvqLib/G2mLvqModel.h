#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"
#include "G2mLvqPrototype.h"
#include "G2mLvqMatch.h"

USING_PART_OF_NAMESPACE_EIGEN

class G2mLvqPrototype;
class G2mLvqModel : public AbstractProjectionLvqModel<G2mLvqModel>
{
	std::vector<G2mLvqPrototype, Eigen::aligned_allocator<G2mLvqPrototype> > prototype;
	double lr_scale_P, lr_scale_B;
	const int classCount;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK, dQdwJ, dQdwK; //vectors of dimension DIMS
	PMatrix dQdP;


public:
	inline int classifyProjectedImpl(Vector2d const & P_unknownPoint) const {
		using namespace std;
		G2mLvqMatch matches(&P_unknownPoint);

		for(int i=0;i<prototype.size(); ++ i)
			matches.AccumulateMatch(prototype[i]);

		assert(matches.match != NULL);
		return matches.match->ClassLabel();
	}

	G2mLvqModel(boost::mt19937 & rng, bool randInit, std::vector<int> protodistribution, MatrixXd const & means);

	size_t MemAllocEstimateImpl() const;
	void learnFromImpl(VectorXd const & newPoint, int classLabel);
	void ClassBoundaryDiagramImpl(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const;
};


