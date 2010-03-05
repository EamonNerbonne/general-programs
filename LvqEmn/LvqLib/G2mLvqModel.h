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

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK, dQdwJ, dQdwK; //vectors of dimension DIMS
	PMatrix dQdP;

	
public:
	virtual size_t MemAllocEstimate() const {
		return 
		sizeof(G2mLvqModel) +
		sizeof(double) * (P.size() + dQdP.size()) +
		sizeof(double) * (vJ.size()*4) + //various temps
		sizeof(G2mLvqPrototype)*prototype.size() + //prototypes; part statically allocated
		sizeof(double) * (prototype.size() * vJ.size()) + //prototypes; part dynamically allocated
		(16/2) * (4+prototype.size()*2);//estimate for alignment mucking.
	}

	G2mLvqModel(boost::mt19937 & rng, bool randInit, std::vector<int> protodistribution, MatrixXd const & means);
	inline int classify(VectorXd const & unknownPoint) const;
	inline int classifyProjected(Vector2d const & unknownProjectedPoint) const;
	void learnFrom(VectorXd const & newPoint, int classLabel);
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const;
	virtual AbstractLvqModel* clone();
};


inline int G2mLvqModel::classifyProjected(Vector2d const & P_unknownPoint) const {
	using namespace std;
	G2mLvqMatch matches(&P_unknownPoint);

	for(int i=0;i<prototype.size(); ++ i)
		matches.AccumulateMatch(prototype[i]);

	assert(matches.match != NULL);
	return matches.match->ClassLabel();
}

inline int G2mLvqModel::classify(VectorXd const & unknownPoint) const{
	using namespace std;
#if EIGEN3
	Vector2d P_unknownPoint;
	P_unknownPoint.noalias() = P * unknownPoint;
#else
	Vector2d P_unknownPoint = (P * unknownPoint).lazy();
#endif
	G2mLvqMatch matches(&P_unknownPoint);

	for(int i=0;i<prototype.size();++i)
		matches.AccumulateMatch(prototype[i]);

	assert(matches.match != NULL);
	return matches.match->ClassLabel();
}
