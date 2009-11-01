#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"

USING_PART_OF_NAMESPACE_EIGEN

class G2mLvqPrototype;
class G2mLvqModel : public AbstractProjectionLvqModel
{
	PMatrix P;
	boost::scoped_array<G2mLvqPrototype> prototype;
	int protoCount;
	double lr_scale_P, lr_scale_B;
	const int classCount;

	//calls dimensionality of input-space DIMS
	//we will preallocate a few vectors to reduce malloc/free overhead.

	VectorXd vJ, vK, dQdwJ, dQdwK; //vectors of dimension DIMS
	PMatrix dQdP;

	//struct trialStruct { trialStruct() {std::cout<<"trialStruct();\n";} ~trialStruct() {std::cout<<"~trialStruct();\n";}	} hidden;
	
public:
	PMatrix const & getProjection() const {return P; }
	G2mLvqPrototype const * Prototypes() const {return prototype.get();}

	G2mLvqModel(std::vector<int> protodistribution, MatrixXd const & means);
	int classify(VectorXd const & unknownPoint) const; //tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	void learnFrom(VectorXd const & newPoint, int classLabel, double learningRate);//tmp must be just as large as unknownPoint, this is a malloc/free avoiding optimization.
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram);
};

