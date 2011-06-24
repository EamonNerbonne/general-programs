#include "stdafx.h"
#include "LvqDataset.h"
#include "shuffle.h"
#include "SmartSum.h"
#include "LvqProjectionModel.h"
#include "utils.h"
#include "PCA.h"
#include "NearestNeighbor.h"
#include "prefetch.h"
using namespace std;
LvqDataset::LvqDataset(Matrix_NN const & points, vector<int> pointLabels, int classCountPar) 
	: points(points)
	, pointLabels(pointLabels)
	, classCount(classCountPar)
{
	assert(points.cols() == int(pointLabels.size()));
	assert(*std::max_element(pointLabels.begin(),pointLabels.end()) < classCount);
	assert(*std::min_element(pointLabels.begin(),pointLabels.end()) >= 0);

	//pointLabels.shrink_to_fit();
}
LvqDataset::LvqDataset(LvqDataset const & src, std::vector<int> const & subset)
	: points(src.points.rows(),subset.size())
	, pointLabels(subset.size())
	, classCount(src.classCount)
{
	for(int i=0;i<(int)subset.size();++i) {
		int pI = subset[i];
		points.col(i).noalias() = src.points.col(pI);
		pointLabels[i] = src.pointLabels[pI];
	}
}

Matrix_NN LvqDataset::ExtractPoints(std::vector<int> const & subset) const {
	Matrix_NN retval(points.rows(),subset.size());
	for(int i=0;i<(int)subset.size();++i) {
		int pI = subset[i];
		retval.col(i).noalias() = points.col(pI);
	}
	return retval;
}

vector<int> LvqDataset::ExtractLabels(std::vector<int> const & subset) const {
	vector<int> retval(subset.size());
	for(int i=0;i<(int)subset.size();++i) {
		int pI = subset[i];
		retval[i] = pointLabels[pI];
	}
	return retval;
}

LvqDataset * LvqDataset::Extract(std::vector<int> const & subset) const {
	return new LvqDataset(*this,subset);
}

Matrix_NN LvqDataset::ComputeClassMeans(std::vector<int> const & subset) const {
	Matrix_NN means( points.rows(), classCount);
	means.setZero();
	boost::scoped_array<int> freq(new int[classCount]);
	for(int i=0;i<classCount;++i) freq[i]=0;

	for(int i=0;i<(int)subset.size();++i) {
		means.col(pointLabels[subset[i]]) += points.col(subset[i]);
		freq[pointLabels[subset[i]]]++;
	}
	for(int i=0;i<classCount;i++) {
		if(freq[i] >0)
			means.col(i) /= double(freq[i]);
	}
	return means;
}

int LvqDataset::NearestNeighborClassify(std::vector<int> const & subset, Vector_N point) const {
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	for(int i=0;i<(int)subset.size();++i) {
		int pI = subset[i];
		double pDist = (points.col(pI) - point).squaredNorm();
		if(pDist < distance) {
			match = pointLabels[pI];
			distance = pDist;
		}
	}
	return match;
}

int LvqDataset::NearestNeighborClassify(std::vector<int> const & subset, Matrix_P projection, Vector_2 & projected_point) const {
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	for(int i=0;i<(int)subset.size();++i) {
		int pI = subset[i];
		double pDist = (projection*points.col(pI) - projected_point).squaredNorm();
		if(pDist < distance) {
			match = pointLabels[pI];
			distance = pDist;
		}
	}
	return match;
}

double LvqDataset::NearestNeighborProjectedErrorRate(std::vector<int> const & neighborhood,LvqDataset const* testData, std::vector<int> const & testSet, Matrix_P projection) const {
	std::vector<int> neighborLabels;
	Matrix_P neighbors;

	neighbors.resize(projection.rows(),neighborhood.size());
	neighborLabels.resize(neighborhood.size());
	for(int i=0;i<(int)neighborhood.size();++i) {
		int pI = neighborhood[i];
		neighbors.col(i).noalias() = projection * points.col(pI);
		neighborLabels[i] = pointLabels[pI];
	}

	NearestNeighbor nn(neighbors);

	Vector_2 testPoint;
	int errs =0;
	for(int i=0;i<(int)testSet.size();++i) {
		int testI = testSet[i];
		testPoint.noalias() = projection * testData->points.col(testI);

		int neighborI=nn.nearestIdx(testPoint);
#if 0
		Matrix_NN::Index neighbor2I;

		(neighbors.colwise() - testPoint).colwise().squaredNorm().minCoeff(&neighbor2I);
		if(neighbor2I!=neighborI) {
			double directDist = (neighbors.col(neighbor2I) - testPoint).squaredNorm();
			double indirectDist = (neighbors.col(neighborI) - testPoint).squaredNorm();
			assert(directDist == indirectDist);
		}
#endif

		if(neighborLabels[neighborI] != testData->pointLabels[testI]) 
			errs++;
	}
	return double(errs) / double(testSet.size());
}

//double LvqDataset::NearestNeighborErrorRate(std::vector<int> const & neighborhood,LvqDataset const* testData, std::vector<int> const & testSet, Matrix_P projection) const {
//	std::vector<int> neighborLabels;
//	Matrix_P neighbors;
//
//	neighbors.resize(projection.rows(),neighborhood.size());
//	neighborLabels.resize(neighborhood.size());
//	for(int i=0;i<(int)neighborhood.size();++i) {
//		int pI = neighborhood[i];
//		neighbors.col(i).noalias() = projection * points.col(pI);
//		neighborLabels[i] = pointLabels[pI];
//	}
//	Vector_2 testPoint;
//	int errs =0;
//	for(int i=0;i<(int)testSet.size();++i) {
//		int testI = testSet[i];
//		testPoint.noalias() = projection * testData->points.col(testI);
//
//		Matrix_NN::Index neighborI;
//
//		(neighbors.colwise() - testPoint).colwise().squaredNorm().minCoeff(&neighborI);
//
//		if(neighborLabels[neighborI] != testData->pointLabels[testI]) 
//			errs++;
//	}
//	return double(errs) / double(testSet.size());
//}

double LvqDataset::NearestNeighborPcaErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet) const {
	return NearestNeighborProjectedErrorRate(neighborhood,testData,testSet,ComputePcaProjection(neighborhood));
}
Matrix_P LvqDataset::ComputePcaProjection(std::vector<int> const & subset) const{
	return PcaProjectInto2d(ExtractPoints(subset));
}


double LvqDataset::NearestNeighborErrorRate(std::vector<int> const & neighborhood, LvqDataset const* testData, std::vector<int> const & testSet) const {
	std::vector<int> neighborLabels;
	Matrix_NN neighbors;

	neighbors.resize(points.rows(),neighborhood.size());
	neighborLabels.resize(neighborhood.size());
	for(int i=0;i<(int)neighborhood.size();++i) {
		int pI = neighborhood[i];
		neighbors.col(i).noalias() = points.col(pI);
		neighborLabels[i] = pointLabels[pI];
	}

	Vector_N testPoint;
	int errs =0;
	for(int i=0;i<(int)testSet.size();++i) {
		int testI = testSet[i];
		testPoint.noalias() =  testData->points.col(testI);

		Matrix_NN::Index neighborI;
		(neighbors.colwise() - testPoint).colwise().squaredNorm().minCoeff(&neighborI);

		if(neighborLabels[neighborI] != testData->pointLabels[testI]) 
			errs++;
	}
	return double(errs) / double(testSet.size());
}


size_t LvqDataset::MemAllocEstimate() const {
	return sizeof(LvqDataset) + sizeof(int) * pointLabels.size()  + sizeof(double)*points.size();
}


void LvqDataset::shufflePoints(boost::mt19937& rng) {
	vector<int> shufLabels(getPointCount());
	Matrix_NN shufPoints(points.rows(),points.cols());

	using boost::scoped_array;
	scoped_array<int> idxs(new int[getPointCount()]);
	makeRandomOrder(rng,idxs.get(),static_cast<int>(points.cols()));

	for(int colI=0;colI<points.cols();++colI) {
		shufPoints.col(idxs[colI]) = points.col(colI);
		shufLabels[idxs[colI]] = pointLabels[colI];
	}
	points = shufPoints;
	pointLabels = shufLabels;
}

bool shouldCollect(unsigned epochsDone) {
	return epochsDone<512 || epochsDone%2==0 && shouldCollect(epochsDone/2);
}

void LvqDataset::TrainModel(int epochs, LvqModel * model, LvqModel::Statistics * statisticsSink, vector<int> const  & trainingSubset, LvqDataset const * testData, std::vector<int> const  & testSubset) const {
	int dims = static_cast<int>(points.rows());
	Vector_N pointA(dims);
	int cacheLines = (dims*sizeof(points(0,0) ) +63)/ 64 ;

	for(int epoch=0; epoch<epochs; ++epoch) {
		prefetchStream(&(model->RngIter()), (sizeof(boost::mt19937) +63)/ 64);
		vector<int> shuffledOrder(trainingSubset);
		shuffle(model->RngIter(), shuffledOrder, shuffledOrder.size());
		BenchTimer t;
		t.start();
		bool collectStats = 	statisticsSink && shouldCollect(model->epochsTrained+1); 

		LvqDatasetStats stats;
		for(int tI=0; tI<(int)shuffledOrder.size(); ++tI) {
			int pointIndex = shuffledOrder[tI];
			int pointClass = pointLabels[pointIndex];
			pointA = points.col(pointIndex);
			prefetch( &points.coeff (0, shuffledOrder[(tI+1)%shuffledOrder.size()]), cacheLines);

			MatchQuality trainingMatchQ = model->learnFrom(pointA, pointClass);
			if(collectStats)
				stats.Add(trainingMatchQ);
		}
		t.stop();
		model->RegisterEpochDone( (int)(1*shuffledOrder.size()), t.value(CPU_TIMER), 1);
		if(collectStats)
			model->AddTrainingStat(*statisticsSink, this, trainingSubset, testData, testSubset, stats);

		model->DoOptionalNormalization();
	}
}

static int triangularIndex(int i, int j) {
	assert(j <= i);
	return j + ((i * (i + 1)) >> 1);
}

void LvqDataset::ExtendByCorrelations() {
#if 1
	int oldDims = int(points.rows());
	points.conservativeResize(oldDims + (oldDims*oldDims + oldDims)/2, NoChange);
#ifndef NDEBUG
	int nextIdx=0;
	for(int i=0;i<oldDims;++i)
		for(int j=0;j<=i;++j) {
			assert(triangularIndex(i,j) == nextIdx);
			nextIdx++;
		}
		assert(nextIdx == (oldDims*oldDims + oldDims)/2);
#endif
		for(int pI=0;pI<points.cols();++pI) 
			for(int i=0;i<oldDims;++i)
				for(int j=0;j<=i;++j) 
					points(oldDims + triangularIndex(i,j), pI) = points(i,pI) * points(j,pI);
#else
	int oldDims = int(points.rows());
	points.conservativeResize(oldDims*2, NoChange);
	for(int i=0;i<oldDims;++i)
		points.row(oldDims +i) = points.row(i).array()*points.row(i).array();
#endif
}

void LvqDataset::NormalizeDimensions() {
	points = points.colwise() - MeanPoint(points);
	Vector_N variance =  points.rowwise().squaredNorm() / (points.cols()-1.0);
	assert((variance.array() >= 0).all());
	vector<int> remapping;
	Vector_N inv_stddev =  variance.array().sqrt().inverse().matrix();

	for(int indim=0;indim<variance.rows();indim++) 
		if(variance(indim) >= std::numeric_limits<LvqFloat>::min()) 
			remapping.push_back(indim);
	if((ptrdiff_t)remapping.size()<points.rows())
		cout<<"Retaining "<< remapping.size() <<" of "<< points.rows()<<" dimensions\n";
	for(int pI=0; pI <points.cols(); pI++) {
		for(int outdim=0; outdim<remapping.size(); outdim++) {
			int indim = remapping[outdim];
			points(outdim,pI) = points(indim,pI) * inv_stddev(indim);
		}
	}
	points.conservativeResize(remapping.size(), Eigen::NoChange);

	//	variance = (variance.array() < std::numeric_limits<LvqFloat>::min()).matrix().cast<LvqFloat>() + variance;
	//	points = variance.array().sqrt().inverse().matrix().asDiagonal() * (points.colwise() - mean);
}

using Eigen::Array2d;

LvqDatasetStats LvqDataset::ComputeCostAndErrorRate(std::vector<int> const & subset, LvqModel const * model) const{
	assert(subset.size() > 0);
	LvqDatasetStats stats;
#ifdef DEBUGHELP
	if(model->sentinal != initSentinal)		throw "Whoops!";
#endif
	for(int i=0;i<(int)subset.size();++i) {
		assert(points.sum() == points.sum());
		MatchQuality matchQ = model->ComputeMatches(points.col(subset[i]), pointLabels[subset[i]]);
#ifdef DEBUGHELP
		if(model->sentinal != initSentinal)		throw "Whoops!";
#endif

		stats.Add(matchQ);
	}
	return stats;
}

Matrix_P LvqDataset::ProjectPoints(LvqProjectionModel const * model) const {
	return model->projectionMatrix() * points;
}

std::vector<int> LvqDataset::GetEverythingSubset() const {
	std::vector<int> idxs((size_t)getPointCount());
	for(int i=0;i<getPointCount();i++) idxs[i]=i; return idxs; 
}

std::vector<int> LvqDataset::GetTrainingSubset(int fold, int foldcount) const {
	if(foldcount==0) 
		return GetEverythingSubset();
	else {
		fold = fold % foldcount;
		int pointCount = getPointCount();
		int foldStart = fold * pointCount / foldcount;
		int foldEnd = (fold+1) * pointCount / foldcount;
		int totalLength = foldStart + pointCount - foldEnd;

		std::vector<int> retval(totalLength);
		int j=0;
		for(int i=0;i<foldStart;++i)
			retval[j++] = i;
		for(int i=foldEnd;i<pointCount;++i)
			retval[j++]=i;
		return retval;
	}
}

int LvqDataset::GetTrainingSubsetSize(int fold, int foldcount) const {
	if(foldcount==0) 
		return getPointCount();

	fold = fold % foldcount;
	int pointCount = getPointCount();
	int foldStart = fold * pointCount / foldcount;
	int foldEnd = (fold+1) * pointCount / foldcount;
	int totalLength = foldStart + pointCount - foldEnd;
	return totalLength;
}


std::vector<int> LvqDataset::GetTestSubset(int fold, int foldcount) const {
	if(foldcount==0) return std::vector<int>();
	fold = fold % foldcount;
	int pointCount = getPointCount();
	int foldStart = fold * pointCount / foldcount;
	int foldEnd = (fold+1) * pointCount / foldcount;
	std::vector<int> retval(foldEnd-foldStart);
	for(size_t i=0;i<retval.size();++i)
		retval[i] = foldStart + (int)i;
	return retval;
}

int LvqDataset::GetTestSubsetSize(int fold, int foldcount) const {
	if(foldcount==0) 
		return 0;

	fold = fold % foldcount;
	int pointCount = getPointCount();
	int foldStart = fold * pointCount / foldcount;
	int foldEnd = (fold+1) * pointCount / foldcount;
	int totalLength = foldEnd - foldStart;
	return totalLength;
}

