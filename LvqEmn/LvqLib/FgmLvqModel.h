#pragma once
#include "LvqProjectionModelBase.h"
#include "FgmLvqPrototype.h"

using namespace Eigen;

class FgmLvqModel : public LvqProjectionModelBase<FgmLvqModel>
{
	typedef std::vector<FgmLvqPrototype, Eigen::aligned_allocator<FgmLvqPrototype> > protoList;

	double totalMuJLr, totalMuKLr, lastAutoPupdate;
	protoList prototype;

	//we will preallocate a few temp vectors to reduce malloc/free overhead.
	Vector_N m_vJ, m_vK;
	mutable Vector_N m_dists;
	mutable Vector_N m_probs;
	Matrix_P m_Pdelta;


	//also sets m_dists and m_probs:
	std::tuple<MatchQuality,double,double,size_t> ComputeMatchesInternal(Vector_2 const & P_point, int label) const {
		double bestJdist = std::numeric_limits<double>::infinity(), bestKdist = std::numeric_limits<double>::infinity();
		size_t bestJ=-1, bestK;
		for(size_t i=0;i<PrototypeCount();i++) {
			double dist = SqrDistanceTo(i, P_point);
			if(label==PrototypeLabel(i)){
				if(dist<bestJdist) {
					bestJdist=dist;
					bestJ = i;
				} 
			} else {
				if(dist<bestKdist) {
					bestKdist=dist;
					bestK = i;
				} 
			}
			m_dists(i) = SqrDistanceTo(i, P_point);
		}
		double minDist = std::min(bestJdist, bestKdist);
		m_probs.block(0,0,m_probs.size(),1).array() = (-0.5*(m_dists.array() - minDist)).exp();
		double probJsum=0.0, probKsum=0.0;
		for(size_t i=0;i<PrototypeCount();i++) {
			if(label==PrototypeLabel(i))
				probJsum+=m_probs(i);
			else
				probKsum+=m_probs(i);
		}


		MatchQuality quality;
		quality.costFunc = (probKsum - probJsum) / (probJsum + probKsum);
		quality.distBad = bestKdist;
		quality.distGood = bestJdist;
		quality.isErr = bestKdist <= bestJdist;
		quality.muK = quality.muJ = probKsum * probJsum / sqr(probJsum + probKsum);
		return std::make_tuple(quality,probJsum,probKsum,bestJ);
	}


protected:
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, LvqDataset const * testSet) const;
	virtual bool IdenticalMu() const {return true;}
	virtual void compensateProjectionUpdate(Matrix_22 U, double scale);



public:
	virtual Matrix_NN PrototypeDistances(Matrix_NN const & points) const;

	static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::FgmModelType;
	//for templates:

	inline int PrototypeLabel(size_t protoIndex) const {return prototype[protoIndex].label();}
	inline int PrototypeCount() const {return static_cast<int>(prototype.size());}
	inline double SqrDistanceTo(size_t protoIndex, Vector_2 const & P_otherPoint) const { return prototype[protoIndex].SqrDistanceTo(P_otherPoint); }

	//end for templates


	FgmLvqModel(LvqModelSettings & initSettings);
	virtual size_t MemAllocEstimate() const;
	virtual int classify(Vector_N const & unknownPoint) const {return classifyProjectedInline(P * unknownPoint);}
	virtual int classifyProjected(Vector_2 const & unknownProjectedPoint) const { return classifyProjectedInline(unknownProjectedPoint);}
	EIGEN_STRONG_INLINE int classifyProjectedInline(Vector_2 const & P_unknownPoint) const {
		double distance(std::numeric_limits<double>::infinity());
		int match(-1);

		for(int i=0;i<int(prototype.size());i++) {
			double curDist = prototype[i].SqrDistanceTo(P_unknownPoint);
			if(curDist < distance) { match=i; distance = curDist; }
		}
		assert( match >= 0 );
		return prototype[match].classLabel;
	}

	MatchQuality ComputeMatches(Vector_N const & unknownPoint, int pointLabel) const { return std::get<0>(ComputeMatchesInternal(P * unknownPoint, pointLabel)); }
	MatchQuality learnFrom(Vector_N const & newPoint, int classLabel);
	virtual LvqModel* clone() const ;

	virtual Matrix_2N GetProjectedPrototypes() const;
	virtual std::vector<int> GetPrototypeLabels() const;
	virtual void DoOptionalNormalization();
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const;
	virtual void CopyTo(LvqModel& target) const{ 
		FgmLvqModel & typedTarget = dynamic_cast<FgmLvqModel&>(target);
		typedTarget = *this;
	}
};
