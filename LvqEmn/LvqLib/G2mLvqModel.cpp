#include "stdafx.h"
#include "G2mLvqModel.h"
#include "utils.h"
#include "G2mLvqMatch.h"
#include "LvqConstants.h"


G2mLvqModel::G2mLvqModel(boost::mt19937 & rng,  bool randInit, std::vector<int> protodistribution, MatrixXd const & means) 
	: AbstractProjectionLvqModel<G2mLvqModel>(means.rows()) 
	, lr_scale_P(LVQ_LrScaleP)
	, lr_scale_B(LVQ_LrScaleB)
	, classCount((int)protodistribution.size())
	, vJ(means.rows())
	, vK(means.rows())
	, dQdwJ(means.rows())
	, dQdwK(means.rows())
	, dQdP(LVQ_LOW_DIM_SPACE,means.rows())
{
	using namespace std;

	if(randInit)
		projectionRandomizeUniformScaled(rng, P);
	else
		P.setIdentity();

	int protoCount = accumulate(protodistribution.begin(),protodistribution.end(),0);
	iterationScaleFactor/=protoCount;
	prototype.resize(protoCount);

	int protoIndex=0;
	for(int label=0; label <(int) protodistribution.size();label++) {
		int labelCount =protodistribution[label];
		for(int i=0;i<labelCount;i++) {
			prototype[protoIndex] = G2mLvqPrototype(rng,false, label, means.col(label) );//TODO:experiment with random projection initialization.
			prototype[protoIndex].ComputePP(P);

			protoIndex++;
		}
	}
	assert( accumulate(protodistribution.begin(), protodistribution.end(), 0)== protoIndex);
}





size_t G2mLvqModel::MemAllocEstimateImpl() const {
	return 
		sizeof(G2mLvqModel) +
		sizeof(double) * (P.size() + dQdP.size()) +
		sizeof(double) * (vJ.size()*4) + //various temps
		sizeof(G2mLvqPrototype)*prototype.size() + //prototypes; part statically allocated
		sizeof(double) * (prototype.size() * vJ.size()) + //prototypes; part dynamically allocated
		(16/2) * (4+prototype.size()*2);//estimate for alignment mucking.
}

