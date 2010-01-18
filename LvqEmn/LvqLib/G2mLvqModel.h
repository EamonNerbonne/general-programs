#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"
#include "G2mLvqPrototype.h"

USING_PART_OF_NAMESPACE_EIGEN

class G2mLvqPrototype;
class G2mLvqModel : public AbstractProjectionLvqModel
{
	PMatrix P;
	std::vector<G2mLvqPrototype, Eigen::aligned_allocator<G2mLvqPrototype> > prototype;
	int protoCount;
	double lr_scale_P, lr_scale_B;
	const int classCount;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK, dQdwJ, dQdwK; //vectors of dimension DIMS
	PMatrix dQdP;

	//struct trialStruct { trialStruct() {std::cout<<"trialStruct();\n";} ~trialStruct() {std::cout<<"~trialStruct();\n";}	} hidden;
	inline int classifyProjectedInternal(Vector2d const & unknownProjectedPoint) const;
	
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

	virtual double iterationScaleFactor() const {return 1.0/protoCount;}
	virtual double projectionNorm() const { return (P.transpose() * P).lazy().diagonal().sum() ;}

	PMatrix const & getProjection() const {return P; }

	G2mLvqModel(std::vector<int> protodistribution, MatrixXd const & means);
	int classify(VectorXd const & unknownPoint) const;
	int classifyProjected(Vector2d const & unknownProjectedPoint) const {return classifyProjectedInternal(unknownProjectedPoint);}
	void learnFrom(VectorXd const & newPoint, int classLabel);
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const;
	virtual AbstractLvqModel* clone();
};

