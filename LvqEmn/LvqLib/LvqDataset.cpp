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
LvqDataset::LvqDataset(Matrix_NN const & points, VectorXi const & pointLabels, int classCountPar) 
	: points(points)
	, pointLabels(pointLabels)
	, m_classCount(classCountPar)
{
	//DBG(this->points.cols());
	//DBG(this->pointLabels.size());
	//DBG(this->pointLabels.maxCoeff());
	//DBG(this->pointLabels.minCoeff());
	//DBG(pointLabels);
	//DBG(this->m_classCount);
	assert(points.cols() == pointLabels.size());
	assert(pointLabels.maxCoeff() < classCountPar);
	assert(pointLabels.minCoeff() >= 0);
	//DBG(this->points.mean());
	//DBG(points.mean());
	//pointLabels.shrink_to_fit();
}
LvqDataset::LvqDataset(LvqDataset const & src, std::vector<int> const & subset)
	: points(src.points.rows(),subset.size())
	, pointLabels((ptrdiff_t)subset.size())
	, m_classCount(src.m_classCount)
{
	//DBG(src.points.mean());
	for(int i=0;i<(int)subset.size();++i) {
		int pI = subset[i];
		points.col(i).noalias() = src.points.col(pI);

		pointLabels(i) = src.pointLabels(pI);
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

Matrix_NN LvqDataset::ComputeClassMeans() const {
	Matrix_NN means = Matrix_NN::Zero(points.rows(), classCount());
	VectorXi freq = VectorXi::Zero(classCount());

	for(ptrdiff_t i=0;i<pointCount();++i) {
		means.col(pointLabels[i]) += points.col(i);
		freq(pointLabels[i])++;
	}
	for(int i=0;i<classCount();i++) {
		if(freq(i) >0)
			means.col(i) /= double(freq(i));
	}
	return means;
}

int LvqDataset::NearestNeighborClassify(Vector_N point) const {
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	for(ptrdiff_t i=0;i<pointCount();++i) {
		double pDist = (points.col(i) - point).squaredNorm();
		if(pDist < distance) {
			match = pointLabels(i);
			distance = pDist;
		}
	}
	return match;
}

int LvqDataset::NearestNeighborClassify(Matrix_P projection, Vector_2 & projected_point) const {
	double distance(std::numeric_limits<double>::infinity());
	int match(-1);

	for(ptrdiff_t i=0;i<pointCount();++i) {
		double pDist = (projection*points.col(i) - projected_point).squaredNorm();
		if(pDist < distance) {
			match = pointLabels(i);
			distance = pDist;
		}
	}
	return match;
}

double LvqDataset::NearestNeighborProjectedErrorRate(LvqDataset const& testData, Matrix_P projection) const {
	Matrix_P neighbors = projection * points;

	NearestNeighbor nn(neighbors);

	Vector_2 testPoint;
	int errs =0;
	for(ptrdiff_t testI=0;testI<testData.pointCount();++testI) {
		testPoint.noalias() = projection * testData.points.col(testI);
		Matrix_NN::Index neighborI = nn.nearestIdx(testPoint);

#ifdef __GNUC__
		 //COMPILER HACK!
		 //g++ 4.6 optimizer somehow borks the nearest neighbor search unless I look at the distances it produces.
		 //this forces it to actually compute the NN.
		Matrix_NN::Index neighbor2I = (neighborI + 1)%pointCount();
		double directDist = (neighbors.col(neighbor2I) - testPoint).squaredNorm();
		double indirectDist = (neighbors.col(neighborI) - testPoint).squaredNorm();
		if(directDist < indirectDist)
			std::cout << "naive:"<<directDist<<"; nn:"<<indirectDist<<"\n";

#endif

		/*

		Matrix_NN::Index neighbor2I;

		(neighbors.colwise() - testPoint).colwise().squaredNorm().minCoeff(&neighbor2I);
		if(neighbor2I!=neighborI) {
			double directDist = (neighbors.col(neighbor2I) - testPoint).squaredNorm();
			double indirectDist = (neighbors.col(neighborI) - testPoint).squaredNorm();
			if(directDist != indirectDist)
				std::cout << "naive:"<<directDist<<"; nn:"<<indirectDist<<"\n";
//			assert(directDist == indirectDist);
		}
*/

		if(pointLabels(neighborI) != testData.pointLabels(testI)) 
			errs++;
	}
	return double(errs) / double(testData.pointCount());
}


double LvqDataset::NearestNeighborPcaErrorRate(LvqDataset const& testData) const {
	return NearestNeighborProjectedErrorRate(testData,ComputePcaProjection());
}
Matrix_P LvqDataset::ComputePcaProjection() const{
	return PcaProjectInto2d(points);
}

Matrix_NN LvqDataset::ComputePcaProjection(int dims) const{
	return PcaProjectIntoNd(points, dims);
}

double LvqDataset::NearestNeighborErrorRate(LvqDataset const& testData) const {
	Vector_N testPoint;
	int errs =0;
	for(ptrdiff_t testI=0;testI<testData.pointCount();++testI) {
		testPoint.noalias() =  testData.points.col(testI);

		Matrix_NN::Index neighborI;
		(points.colwise() - testPoint).colwise().squaredNorm().minCoeff(&neighborI);

		if(pointLabels(neighborI) != testData.pointLabels(testI)) 
			errs++;
	}
	return double(errs) / double(testData.pointCount());
}


size_t LvqDataset::MemAllocEstimate() const {
	return sizeof(LvqDataset) + sizeof(int) * pointLabels.size()  + sizeof(double)*points.size();
}


void LvqDataset::shufflePoints(std::mt19937& rng) {
	VectorXi shufLabels(pointCount());
	//DBG(shufLabels.size());
	Matrix_NN shufPoints(points.rows(),points.cols());
	//DBG(shufPoints.size());
	using boost::scoped_array;

	scoped_array<int> idxs(new int[pointCount()]);
	makeRandomOrder(rng,idxs.get(),static_cast<int>(points.cols()));

	for(int colI=0;colI<points.cols();++colI) {
		shufPoints.col(idxs[colI]).noalias() = points.col(colI);
		shufLabels(idxs[colI]) = pointLabels(colI);
	}
	points.noalias() = shufPoints;
	//pointLabels.noalias() = shufLabels;
	for(int i=0;i<points.cols();++i) pointLabels(i) = shufLabels(i);//VS11 workaround
//	DBG(pointLabels);
}

bool shouldCollect(unsigned epochsDone) {
	return epochsDone<256 || (epochsDone%2==0 && shouldCollect(epochsDone/2));
}

void LvqDataset::TrainModel(int epochs, LvqModel & model, LvqModel::Statistics * statisticsSink, LvqDataset const * testData, int* labelOrderSink, bool sortedTrain) const {
	int dims = static_cast<int>(points.rows());
	Vector_N pointA(dims);
	int cacheLines = (dims*sizeof(points(0,0) ) +63)/ 64 ;

	size_t labelOrderSinkIdx=0;

	for(int epoch=0; epoch<epochs; ++epoch) {
		prefetchStream(&(model.RngIter()), (sizeof(std::mt19937) +63)/ 64);
		vector<int> shuffledOrder(GetTestSubset(0,1));
		shuffle(model.RngIter(), shuffledOrder, (unsigned)shuffledOrder.size());
		if(sortedTrain) {
			Vector_N sortBy;
			if(dynamic_cast<LvqProjectionModel*>(&model)) {
				Matrix_P projected = ProjectPoints(dynamic_cast<LvqProjectionModel&>(model));
				sortBy = (projected).row(0).transpose();
			} else {
				sortBy = (PcaProjectInto2d(points) * points).row(0).transpose();
			}
			sort(shuffledOrder.begin(),shuffledOrder.end(), [&sortBy, this] (int pIa, int pIb) { return pointLabels[pIa] == pointLabels[pIb] ? sortBy(pIa) < sortBy(pIb) : pointLabels[pIa] < pointLabels[pIb]; });
		}
		
		BenchTimer t;
		t.start();
		bool collectStats = 	statisticsSink && shouldCollect(model.epochsTrained+1); 

		for(int tI=0; tI<(int)shuffledOrder.size(); ++tI) {
			int pointIndex = shuffledOrder[tI];
			int pointClass = pointLabels[pointIndex];
			if(labelOrderSink)
				labelOrderSink[labelOrderSinkIdx++] = pointClass;
			pointA = points.col(pointIndex);
			prefetch( &points.coeff (0, shuffledOrder[(tI+1)%shuffledOrder.size()]), cacheLines);
			model.learnFrom(pointA, pointClass);
		}
		t.stop();
		model.RegisterEpochDone( (int)(1*shuffledOrder.size()), t.value(CPU_TIMER), 1);
		if(collectStats)
			model.AddTrainingStat(*statisticsSink, this,  testData);

		model.DoOptionalNormalization();
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

std::pair<Vector_N,Vector_N> LvqDataset::NormalizationParameters() const {
	auto mean = MeanPoint(points);
	Vector_N variance =  (points.colwise() - mean).rowwise().squaredNorm() / (points.cols()-1.0);

	return make_pair(mean,variance);
}
void LvqDataset::ApplyNormalization(std::pair<Vector_N,Vector_N> pars, bool normalizeByScaling) {
	
	auto mean = pars.first;
	auto variance = pars.second;

	points = points.colwise() - mean;

	//assert((variance.array() >= 0).all());
	vector<int> remapping;
	Vector_N inv_stddev =  variance.array().sqrt().inverse().matrix();

	for(int indim=0;indim<variance.rows();indim++) 
		if(variance(indim) >= std::numeric_limits<LvqFloat>::min()) 
			remapping.push_back(indim);

	if(normalizeByScaling)
		inv_stddev = Vector_N::Ones(inv_stddev.size()) * (1.0/sqrt(variance.sum() / remapping.size()));

	if((ptrdiff_t)remapping.size()<points.rows()) {
		cout<<"Retaining "<< remapping.size() <<" of "<< points.rows()<<" dimensions\n";
		cout.flush();
	}
	for(size_t pI=0; pI < static_cast<size_t>(points.cols()); pI++) {
		for(size_t outdim=0; outdim<remapping.size(); outdim++) {
			int indim = remapping[outdim];
			points(outdim,pI) = points(indim,pI) * inv_stddev(indim);
		}
	}
	points.conservativeResize(remapping.size(), Eigen::NoChange);

	//	variance = (variance.array() < std::numeric_limits<LvqFloat>::min()).matrix().cast<LvqFloat>() + variance;
	//	points = variance.array().sqrt().inverse().matrix().asDiagonal() * (points.colwise() - mean);
}

using Eigen::Array2d;

LvqDatasetStats LvqDataset::ComputeCostAndErrorRate(LvqModel const & model) const{
	LvqDatasetStats stats;
	Vector_N point(points.rows());
	for(int i=0;i<pointCount();++i) {
		point = points.col(i);
		MatchQuality matchQ = model.ComputeMatches(point, pointLabels(i));

		stats.Add(matchQ);
	}
	return stats;
}

Matrix_P LvqDataset::ProjectPoints(LvqProjectionModel const & model) const {
	return model.projectionMatrix() * points;
}

std::vector<int> LvqDataset::GetTrainingSubset(int fold, int foldcount) const {

	if(foldcount==0) 
		return GetTestSubset(0,1);
	else {
		fold = fold % foldcount;
		ptrdiff_t foldStart = fold * pointCount() / foldcount;
		ptrdiff_t foldEnd = (fold+1) * pointCount() / foldcount;
		ptrdiff_t totalLength = foldStart + pointCount() - foldEnd;

		std::vector<int> retval(totalLength);
		int j=0;
		for(ptrdiff_t i=0;i<foldStart;++i)
			retval[j++] = (int)i;
		for(ptrdiff_t i=foldEnd;i<pointCount();++i)
			retval[j++]=(int)i;
		return retval;
	}
}

std::vector<int> LvqDataset::GetTestSubset(int fold, int foldcount) const {
	if(foldcount==0) return std::vector<int>();
	fold = fold % foldcount;
	ptrdiff_t foldStart = fold * pointCount() / foldcount;
	ptrdiff_t foldEnd = (fold+1) * pointCount() / foldcount;
	std::vector<int> retval(foldEnd-foldStart);
	for(ptrdiff_t i=0;i<(ptrdiff_t)retval.size();++i)
		retval[i] =  (int)(foldStart + i);
	return retval;
}

std::vector<int> LvqDataset::InRandomOrder(std::mt19937& rng ) const { vector<int> order(GetTestSubset(0,1)); shuffle(rng,order, (unsigned)order.size()); return order; }


LvqDataset LvqDataset::ExtendWithOther(LvqDataset const & other) const {
	assert(other.classCount() == classCount());
	assert(other.pointCount() == pointCount());
	assert((pointLabels - other.pointLabels).isZero());

	size_t newDimCount = dimCount() + other.dimCount();
	Matrix_NN stackedPoints = Matrix_NN(newDimCount,pointCount());
	stackedPoints.topRows(dimCount()) = points;
	stackedPoints.bottomRows(other.dimCount()) = other.points;
	return LvqDataset(stackedPoints, pointLabels, classCount());
}

LvqDataset LvqDataset::CreateLogDistDataset(LvqModel const & model) const  {
	Matrix_NN protoDistances = (model.PrototypeDistances(points).array() + std::numeric_limits<double>::min()).log().matrix();
	//protoDistances = protoDistances.rowwise() - protoDistances.colwise().minCoeff();
	assert(points.cols()==protoDistances.cols());
	return LvqDataset(protoDistances, pointLabels, classCount());
}

LvqDataset LvqDataset::CreateInvSqrtDistDataset(LvqModel const & model) const {
	Matrix_NN protoDistances = (model.PrototypeDistances(points).array().sqrt() + 0.01).inverse().matrix();
	 
	assert(points.cols()==protoDistances.cols());
	return LvqDataset(protoDistances, pointLabels, classCount());

}


LvqDataset LvqDataset::CreateQrProjectedDataset(LvqModel const & model) const {
	Matrix_NN modelProj = model.GetCombinedTransforms(); //the model projections may be many and some may be linearly dependant or at least non-orthogonal.  To avoid adding unnecessary dimensions, I want to get rid of the gunk.

	auto qr_decomp = modelProj.fullPivHouseholderQr(); // I don't care about the unitary matrix
	auto R = qr_decomp.matrixQR().triangularView<Upper>();
	auto P = qr_decomp.colsPermutation();
	//now we have modelProj == Q R P^T with Q unitary and thus uninteresting.

	modelProj = (R.toDenseMatrix() * P.transpose()).topRows(min(modelProj.rows(),modelProj.cols()));
	normalizeProjection(modelProj);
	Matrix_NN projectionFeatures = modelProj * points;
	return LvqDataset(projectionFeatures, pointLabels,classCount());
}



std::pair<LvqDataset*,LvqDataset*> LvqDataset::ExtendUsingModel(LvqDataset const * testdataset,LvqDataset const * extendDataset, LvqDataset const * extendTestdataset,  LvqModel const & model) const {
	LvqDataset logDs = this->CreateLogDistDataset(model);
	auto logDsNorm = logDs.NormalizationParameters();
	logDs.ApplyNormalization(logDsNorm, false);

	//LvqDataset projDs = CreateQrProjectedDataset(model);
	//auto projDsNorm = projDs.NormalizationParameters();
	//projDs.ApplyNormalization(projDsNorm, false);
	
	LvqDataset combinedDs = extendDataset==nullptr?logDs:extendDataset->ExtendWithOther(logDs);

	if(!testdataset) 
		return make_pair(new LvqDataset(combinedDs), (LvqDataset*)nullptr);
	

	LvqDataset testLogDs = testdataset->CreateLogDistDataset(model);
	testLogDs.ApplyNormalization(logDsNorm, false);

	//LvqDataset testProjDs = testdataset->CreateQrProjectedDataset(model);
	//testProjDs.ApplyNormalization(projDsNorm, false);
	
	LvqDataset testCombinedDs = extendTestdataset==nullptr?testLogDs: extendTestdataset->ExtendWithOther(testLogDs);
	
	return make_pair(new LvqDataset(combinedDs), new LvqDataset(testCombinedDs));

/*
	Matrix_NN protoDistances = model.PrototypeDistances(points);
	assert(points.cols()==protoDistances.cols());

	
	SmartSum<Eigen::Dynamic> distribution(protoDistances.rows());
	for(ptrdiff_t colIndex=0; colIndex < points.cols();++colIndex)
		distribution.CombineWith(protoDistances.col(colIndex).array(), 1.0);
	


	//Matrix_NN newPoints(points.rows() + protoDistances.rows(), points.cols());
	//newPoints.topRows(points.rows()).noalias() = points;
	//newPoints.bottomRows(protoDistances.rows()).noalias() = distribution.GetSampleVariance().sqrt().inverse().matrix().asDiagonal() * (protoDistances.colwise() - distribution.GetMean().matrix());
	//return new LvqDataset(newPoints, pointLabels, classCount);//classcount, and labels unchanged!

	Matrix_NN modelProj = model.GetCombinedTransforms();
	//the model projections may be many and some may be linearly dependant or at least non-orthogonal.  To avoid adding unnecessary dimensions, I want to get rid of the gunk.


	auto qr_decomp = modelProj.fullPivHouseholderQr(); // I don't care about the unitary matrix
	auto R = qr_decomp.matrixQR().triangularView<Upper>();
	auto P = qr_decomp.colsPermutation();
	//now we have modelProj == Q R P^T with Q unitary and thus uninteresting.
#if 0
	Matrix_NN Rdebug = R, Pdebug=P;
	std::cout <<"\nR: \n" << Rdebug ;
	std::cout <<"\nP: \n" << Pdebug <<"\n";
	Matrix_NN altProj = Rdebug * P.transpose();
	cout<<"\norigRows: "<<modelProj.rows()<<"; new rows:"<<altProj.rows()<<"\n";
	std::cout <<"mp: "<< (modelProj*points.col(0)).norm() << "; "<<(altProj*points.col(0)).norm() <<"\n";
	std::cout.flush();
#endif
	modelProj = (R.toDenseMatrix() * P.transpose()).topRows(min(modelProj.rows(),modelProj.cols()));
	normalizeProjection(modelProj);


	Matrix_NN distanceFeatures = distribution.GetSampleVariance().sqrt().inverse().matrix().asDiagonal() * (protoDistances.colwise() - distribution.GetMean().matrix());


	Matrix_NN projectionFeatures = modelProj * points;
	
	Vector_N pFmean = projectionFeatures.rowwise().mean();
	projectionFeatures.colwise() -= pFmean;

	double pFvar=0;
	for(ptrdiff_t colIndex=0;colIndex < points.cols();++colIndex)
		pFvar += projectionFeatures.col(colIndex).squaredNorm();
	pFvar /= double(points.cols() * projectionFeatures.rows());


	Matrix_NN newPoints(modelProj.rows()+ distanceFeatures.rows(), points.cols());
	newPoints.topRows(modelProj.rows()).noalias() = projectionFeatures/sqrt(pFvar);
	newPoints.bottomRows(distanceFeatures.rows()).noalias() = distanceFeatures;


	if(testdataset==nullptr)
		return std::make_pair(new LvqDataset(newPoints, pointLabels, classCount()), (LvqDataset*)nullptr);

	Matrix_NN protoDistancesTest = model.PrototypeDistances(testdataset->points);
	Matrix_NN distanceFeaturesTest	= distribution.GetSampleVariance().sqrt().inverse().matrix().asDiagonal() * (protoDistancesTest.colwise() - distribution.GetMean().matrix());
	
	Matrix_NN projectionFeaturesTest = (modelProj * testdataset->points).colwise() - pFmean;

	Matrix_NN newPointsTest = Matrix_NN(modelProj.rows()+ distanceFeaturesTest.rows(), testdataset->points.cols());
	newPointsTest.topRows(modelProj.rows()).noalias() = projectionFeaturesTest/sqrt(pFvar);
	newPointsTest.bottomRows(distanceFeaturesTest.rows()).noalias() = distanceFeaturesTest;

	return std::make_pair(new LvqDataset(newPoints, pointLabels, classCount()), new LvqDataset(newPointsTest,testdataset->pointLabels, classCount()));*/
}
