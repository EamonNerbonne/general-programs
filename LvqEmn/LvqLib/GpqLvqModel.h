#pragma once
#include "LvqProjectionModelBase.h"

class GpqLvqPrototype
{
	friend class GpqLvqModel;
	Matrix_22 B;
	int classLabel; //only set during initialization.
	Vector_2 P_point;
public:
	inline int label() const {return classLabel;}
	inline Matrix_22 const & matB() const {return B;}
	inline Vector_2 const & projectedPosition() const{return P_point;}
	GpqLvqPrototype() : classLabel(-1) {}
	GpqLvqPrototype(Matrix_22 const & Binit, int protoLabel, Vector_2 const & initialVal) : B(Binit) , classLabel(protoLabel), P_point(initialVal) {}
	inline LvqFloat SqrDistanceTo(Vector_2 const & P_testPoint) const { Vector_2 P_Diff = P_testPoint - P_point; return (B * P_Diff).squaredNorm(); }
	inline LvqFloat SqrRawDistanceTo(Vector_2 const & P_testPoint) const { return (P_testPoint - P_point).squaredNorm(); }
	EIGEN_MAKE_ALIGNED_OPERATOR_NEW
};

class GpqLvqModel : public LvqProjectionModelBase<GpqLvqModel>
{
	typedef std::vector<GpqLvqPrototype, Eigen::aligned_allocator<GpqLvqPrototype> > protoList;
	double totalMuJLr, totalMuKLr;
	protoList prototype;
	//Matrix_P Pnew;
	std::vector<CorrectAndWorstMatches::MatchOk> ngMatchCache;
	//we will preallocate a few temp vectors to reduce malloc/free overhead.
	void NormalizeBoundaries();
protected:
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet, std::vector<int>const & trainingSubset, LvqDataset const * testSet, std::vector<int>const & testSubset) const;


public:
	virtual Matrix_NN PrototypeDistances(Matrix_NN const & points) const;


	static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::GpqModelType;
	//for templates:

	inline int PrototypeLabel(int protoIndex) const {return prototype[protoIndex].label();}
	inline int PrototypeCount() const {return static_cast<int>(prototype.size());}
	inline double SqrDistanceTo(int protoIndex, Vector_2 const & P_otherPoint) const { return prototype[protoIndex].SqrDistanceTo(P_otherPoint); }

	//end for templates

	GpqLvqModel(LvqModelSettings & initSettings);
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

	MatchQuality learnFrom(Vector_N const & newPoint, int classLabel);
	virtual LvqModel* clone() const ;

	virtual Matrix_2N GetProjectedPrototypes() const;
	virtual std::vector<int> GetPrototypeLabels() const;
	virtual void DoOptionalNormalization();
	
	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const;
	virtual void CopyTo(LvqModel& target) const { 
		GpqLvqModel & typedTarget = dynamic_cast<GpqLvqModel&>(target);
		typedTarget = *this;
	}
};