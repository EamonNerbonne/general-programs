#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"
#include "G2mLvqPrototype.h"

using namespace Eigen;

class G2mLvqModel : public AbstractProjectionLvqModel
{
	std::vector<G2mLvqPrototype, Eigen::aligned_allocator<G2mLvqPrototype> > prototype;
	double lr_scale_P, lr_scale_B;

	//we will preallocate a few temp vectors to reduce malloc/free overhead.
	VectorXd m_vJ, m_vK;

	struct G2mLvqGoodBadMatch {
		Vector2d const* projectedPoint;

		int actualClassLabel;

		double distanceGood, distanceBad;
		G2mLvqPrototype const *good;
		G2mLvqPrototype const *bad;


		inline G2mLvqGoodBadMatch(Vector2d const * projectedTestPoint, int classLabel)
			: projectedPoint(projectedTestPoint)
			, actualClassLabel(classLabel)
			, distanceGood(std::numeric_limits<double>::infinity()) 
			, distanceBad(std::numeric_limits<double>::infinity()) 
			, good(NULL)
			, bad(NULL)
		{ }

		double CostFunc() const{return (distanceGood - distanceBad)/(distanceGood+distanceBad);	}
		bool IsErr()const {return distanceGood > distanceBad;}

		inline void AccumulateMatch(G2mLvqPrototype const & option) {
			double optionDist = option.SqrDistanceTo(*projectedPoint);
			assert(optionDist > 0);
			assert(optionDist < std::numeric_limits<double>::infinity());
			if(option.label() == actualClassLabel) {
				if(optionDist < distanceGood) {
					good = &option;
					distanceGood = optionDist;
				}
			} else {
				if(optionDist < distanceBad) {
					bad = &option;
					distanceBad = optionDist;
				}
			}
		}
	};




	inline int classifyProjectedInternal(Vector2d const & P_unknownPoint) const {
		double distance(std::numeric_limits<double>::infinity());
		int match(-1);

		for(int i=0;i<prototype.size();i++) {
			double curDist = prototype[i].SqrDistanceTo(P_unknownPoint);
			if(curDist < distance) { match=i; distance = curDist; }
		}
		assert( match >= 0 );
		return prototype[match].classLabel;
	}

	inline int classifyInternal(VectorXd const & unknownPoint) const { return classifyProjectedInternal(P * unknownPoint); }
public:

	G2mLvqModel(boost::mt19937 & rngParams,boost::mt19937 & rngIter, bool randInit, std::vector<int> protodistribution, MatrixXd const & means);
	virtual size_t MemAllocEstimate() const;
	int classify(VectorXd const & unknownPoint) const {return classifyInternal(unknownPoint);}
    void computeCostAndError(VectorXd const & unknownPoint, int pointLabel,bool&err,double&cost) const;
	virtual VectorXd otherStats() const; 
	int classifyProjected(Vector2d const & unknownProjectedPoint) const { return classifyProjectedInternal(unknownProjectedPoint);}
	void learnFrom(VectorXd const & newPoint, int classLabel, bool *wasError, double* hadCost);
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const;
	virtual AbstractLvqModel* clone();

	virtual MatrixXd GetProjectedPrototypes() const;
	virtual std::vector<int> GetPrototypeLabels() const;
};
