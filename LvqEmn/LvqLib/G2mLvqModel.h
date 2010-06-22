#pragma once
#include "stdafx.h"
#include "LvqProjectionModelBase.h"
#include "G2mLvqPrototype.h"

using namespace Eigen;

class G2mLvqModel : public LvqProjectionModelBase<G2mLvqModel>
{
	typedef std::vector<G2mLvqPrototype, Eigen::aligned_allocator<G2mLvqPrototype> > protoList;

	protoList prototype;
	double lr_scale_P, lr_scale_B;

	//we will preallocate a few temp vectors to reduce malloc/free overhead.
	VectorXd m_vJ, m_vK;

public:
		//for templates:

	inline int PrototypeLabel(int protoIndex) const {return prototype[protoIndex].label();}
	inline int PrototypeCount() const {return static_cast<int>(prototype.size());}
	inline double SqrDistanceTo(int protoIndex, Vector2d const & P_otherPoint) const { return prototype[protoIndex].SqrDistanceTo(P_otherPoint); }

//end for templates


	G2mLvqModel(boost::mt19937 & rngParams,boost::mt19937 & rngIter, bool randInit, std::vector<int> protodistribution, MatrixXd const & means);
	virtual size_t MemAllocEstimate() const;
	virtual VectorXd otherStats() const; 
	virtual int classify(VectorXd const & unknownPoint) const {return classifyProjectedInline(P * unknownPoint);}
	virtual int classifyProjected(Vector2d const & unknownProjectedPoint) const { return classifyProjectedInline(unknownProjectedPoint);}
	inline int classifyProjectedInline(Vector2d const & P_unknownPoint) const {
		double distance(std::numeric_limits<double>::infinity());
		int match(-1);

		for(int i=0;i<prototype.size();i++) {
			double curDist = prototype[i].SqrDistanceTo(P_unknownPoint);
			if(curDist < distance) { match=i; distance = curDist; }
		}
		assert( match >= 0 );
		return prototype[match].classLabel;
	}

	GoodBadMatch learnFrom(VectorXd const & newPoint, int classLabel);
	virtual LvqModel* clone() const ;

	MatrixXd GetProjectedPrototypes() const;
	std::vector<int> GetPrototypeLabels() const;
};
