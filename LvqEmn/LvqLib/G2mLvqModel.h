#pragma once
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
protected:
	virtual void AppendTrainingStatNames(std::vector<std::wstring> & retval) const;
	virtual void AppendOtherStats(std::vector<double> & stats, LvqDataset const * trainingSet,  std::vector<int>const & trainingSubset, LvqDataset const * testSet,  std::vector<int>const & testSubset) const;


public:
	static const LvqModelSettings::LvqModelType ThisModelType = LvqModelSettings::G2mModelType;
		//for templates:

	inline int PrototypeLabel(int protoIndex) const {return prototype[protoIndex].label();}
	inline int PrototypeCount() const {return static_cast<int>(prototype.size());}
	inline double SqrDistanceTo(int protoIndex, Vector2d const & P_otherPoint) const { return prototype[protoIndex].SqrDistanceTo(P_otherPoint); }

//end for templates


	G2mLvqModel(LvqModelSettings & initSettings);
	virtual size_t MemAllocEstimate() const;
	virtual int classify(VectorXd const & unknownPoint) const {return classifyProjectedInline(P * unknownPoint);}
	virtual int classifyProjected(Vector2d const & unknownProjectedPoint) const { return classifyProjectedInline(unknownProjectedPoint);}
	EIGEN_STRONG_INLINE int classifyProjectedInline(Vector2d const & P_unknownPoint) const {
		double distance(std::numeric_limits<double>::infinity());
		int match(-1);

		for(int i=0;i<int(prototype.size());i++) {
			double curDist = prototype[i].SqrDistanceTo(P_unknownPoint);
			if(curDist < distance) { match=i; distance = curDist; }
		}
		assert( match >= 0 );
		return prototype[match].classLabel;
	}

	GoodBadMatch learnFrom(VectorXd const & newPoint, int classLabel);
	virtual LvqModel* clone() const ;

	virtual MatrixXd GetProjectedPrototypes() const;
	virtual std::vector<int> GetPrototypeLabels() const;

	virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, LvqProjectionModel::ClassDiagramT & classDiagram) const;
};
