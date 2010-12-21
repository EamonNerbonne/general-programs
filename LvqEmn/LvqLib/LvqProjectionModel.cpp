#include "stdafx.h"
#include "LvqProjectionModel.h"
#include "LvqDataset.h"
#include "utils.h"
#include "RandomMatrix.h"
void LvqProjectionModel::AppendTrainingStatNames(std::vector<std::wstring> & retval) const { 
	LvqModel::AppendTrainingStatNames(retval);
	retval.push_back(L"Projection Norm|norm|Projection Matrix");
	if(settings.TrackProjectionQuality)
		retval.push_back(L"Projected NN Error Rate|error rate|Projection Quality");
}
void LvqProjectionModel::AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const { 
	LvqModel::AppendOtherStats(stats,trainingSet,trainingSubset,testSet,testSubset);
	stats.push_back(projectionSquareNorm(P));
	if(settings.TrackProjectionQuality)
		stats.push_back(trainingSet ? trainingSet->NearestNeighborErrorRate(trainingSubset,testSet,testSubset,this->P) : 0.0);
}

void randomProjectionMatrix(boost::mt19937 & rngParams, PMatrix & mat);

inline void randomProjectionMatrix(boost::mt19937 & rngParams, PMatrix & mat) {
	RandomMatrixInit(rngParams,mat,0.0,1.0);
	Eigen::JacobiSVD<PMatrix> svd(mat, Eigen::ComputeThinU | Eigen::ComputeThinV);
	if(mat.rows()>mat.cols())
		mat.noalias() = svd.matrixU();
	else
		mat.noalias() = svd.matrixV().transpose();
#ifndef NDEBUG
	for(int r=0;r<mat.rows();r++){
		for(int r0=0;r0<mat.rows();r0++){
			double dotprod = mat.row(r).dot(mat.row(r0));
			if(r==r0)
				assert(fabs(dotprod-1.0) <= std::numeric_limits<double>::epsilon()*mat.cols());
			else 
				assert(fabs(dotprod) <= std::numeric_limits<double>::epsilon()*mat.cols());
		}
	}
#endif
}



LvqProjectionModel::LvqProjectionModel(LvqModelSettings & initSettings) 
	: LvqModel(initSettings)
	, P(LVQ_LOW_DIM_SPACE, initSettings.Dimensions()) 
{
	if(initSettings.RandomInitialProjection)
//		projectionRandomizeUniformScaled(initSettings.RngParams, P);
		randomProjectionMatrix(initSettings.RngParams, P);
	else
		P = initSettings.pcaTransform;
	//		P.setIdentity();
}


