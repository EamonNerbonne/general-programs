#pragma once
#include "stdafx.h"
#include "AbstractLvqModel.h"
#include "utils.h"
#include "LvqConstants.h"

USING_PART_OF_NAMESPACE_EIGEN


template <int _OutDims, int _InDims, int _MaxOutDims = _OutDims, int _MaxInDims = _InDims>
class G2mLvqModel : public AbstractProjectionLvqModel
{
public:
	typedef Matrix<double, _OutDims,_OutDims,0,_MaxOutDims,_MaxOutDims> TMatrixB;
	typedef Matrix<double, _InDims, 1,0,_MaxInDims,1> TVectorIn;
	typedef Matrix<double, _OutDims, 1,0,_MaxOutDims,1> TVectorOut;
	typedef Matrix<double, _OutDims,_InDims,0,_MaxOutDims,_MaxInDims> TMatrixP;
	typedef Eigen::Map<TVectorIn,Eigen::Aligned> TVectorInM;
private:

	class G2mLvqPrototype
	{
		friend class G2mLvqModel;
		TMatrixB B;
		TVectorIn point;
		int classLabel; //only set during initialization.
		//tmps:
		TVectorOut P_point;
		EIGEN_STRONG_INLINE void ComputePP( TMatrixP const & P) {
	#if EIGEN3
			P_point.noalias() = P  * point;
	#else
			P_point = (P  * point).lazy();
	#endif
		}

	public:
		inline int ClassLabel() const {return classLabel;}
		inline TMatrixB const & matB() const {return B;}
		inline TVectorIn const & position() const{return point;}
		inline G2mLvqPrototype() : classLabel(-1) {}
		inline G2mLvqPrototype(boost::mt19937 & rng, bool randInit, int protoLabel, TVectorIn const & initialVal) 
			: point(initialVal) 
			, classLabel(protoLabel)
		{
			if(randInit)
				projectionRandomizeUniformScaled(rng, B);	
			else 
				B.setIdentity();
		}

		inline double SqrDistanceTo(TVectorOut const & P_testPoint) const {
			TVectorOut P_Diff;
	#if EIGEN3
			P_Diff.noalias() = P_testPoint - P_point;
			return (B * P_Diff).squaredNorm();//waslazy
	#else
			P_Diff = (P_testPoint - P_point).lazy();
			return (B * P_Diff).lazy().squaredNorm();
	#endif
		}

		EIGEN_MAKE_ALIGNED_OPERATOR_NEW
	};


	struct G2mLvqGoodBadMatch {
		TVectorOut const& projectedPoint;
		int actualClassLabel;
		double distanceGood, distanceBad;
		G2mLvqPrototype const * good,  *bad;

		inline G2mLvqGoodBadMatch(TVectorOut const & projectedTestPoint, int classLabel)
			: projectedPoint(projectedTestPoint)
			, actualClassLabel(classLabel)
			, distanceGood(std::numeric_limits<double>::infinity()) 
			, distanceBad(std::numeric_limits<double>::infinity()) 
			, good(NULL), bad(NULL)
		{ }

		inline void AccumulateMatch(G2mLvqPrototype const & option) {
			double optionDist = option.SqrDistanceTo(projectedPoint);
			assert(optionDist > 0);
			assert(optionDist < std::numeric_limits<double>::infinity());
			if(option.ClassLabel() == actualClassLabel) {
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


	struct G2mLvqMatch {
		TVectorOut const & P_testPoint;
		double distance;
		G2mLvqPrototype const * match;

		inline G2mLvqMatch(TVectorOut const & P_testPoint)
			: P_testPoint(P_testPoint)
			, distance(std::numeric_limits<double>::infinity()) 
			, match(NULL)
		{ }

		inline void AccumulateMatch(G2mLvqPrototype const & option) {
			double optionDist = option.SqrDistanceTo(P_testPoint);
			assert(optionDist > 0);
			assert(optionDist < std::numeric_limits<double>::infinity());
			if(optionDist < distance) {
				match = &option;
				distance = optionDist;
			}
		}
	};



	std::vector<G2mLvqPrototype, Eigen::aligned_allocator<G2mLvqPrototype> > prototype;
	double lr_scale_P, lr_scale_B;
	const int classCount;

	//we will preallocate a few temp vectors to reduce malloc/free overhead.
	TVectorIn m_vJ, m_vK;

	template <typename DerivedMatrix>
	inline int classifyProjectedInternal(MatrixBase<DerivedMatrix> const & P_unknownPoint) const {
		G2mLvqMatch matches(P_unknownPoint);
		for(int i=0;i<prototype.size(); ++ i)	matches.AccumulateMatch(prototype[i]);
		assert(matches.match != NULL);
		return matches.match->ClassLabel();
	}

	template <typename DerivedMatrix>
	inline int classifyInternal(MatrixBase<DerivedMatrix> const & unknownPoint) const { return classifyProjectedInternal(P * unknownPoint); }

public:
	//G2mLvqModel(boost::mt19937 & rng, bool randInit, std::vector<int> protodistribution, MatrixXd const & means);
	int classify(VectorXd const & unknownPoint) const {return classifyInternal(unknownPoint);}
	int classifyProjected(Vector2d const & unknownProjectedPoint) const { return classifyProjectedInternal(unknownProjectedPoint);}
	//virtual size_t MemAllocEstimate() const;
	//virtual void learnFrom(VectorXd const & newPoint, int classLabel);
	//virtual void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const;
	//virtual AbstractLvqModel* clone();


	G2mLvqModel(boost::mt19937 & rng,  bool randInit, std::vector<int> protodistribution, MatrixXd const & means) 
		: AbstractProjectionLvqModel(means.rows()) 
		, lr_scale_P(LVQ_LrScaleP)
		, lr_scale_B(LVQ_LrScaleB)
		, classCount((int)protodistribution.size())
		, m_vJ(means.rows())
		, m_vK(means.rows())
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

	void learnFrom(TVectorIn const & trainPoint, int trainLabel) {
		using namespace std;
		//double learningRate = getLearningRate();
		//incLearningIterationCount();
		double learningRate = stepLearningRate();

		double lr_point = learningRate,
			lr_P = learningRate * this->lr_scale_P,
			lr_B = learningRate * this->lr_scale_B; 

		assert(lr_P>=0  &&  lr_B>=0  &&  lr_point>=0);

	#if EIGEN3
		TVectorOut projectedTrainPoint = P * trainPoint;
	#else
		TVectorOut projectedTrainPoint = (P * trainPoint).lazy();
	#endif

		G2mLvqGoodBadMatch matches(projectedTrainPoint, trainLabel);

		for(int i=0;i<prototype.size();i++)
			matches.AccumulateMatch(prototype[i]);

		assert(matches.good !=NULL && matches.bad!=NULL);
		//now matches.good is "J" and matches.bad is "K".

		double mu_J = -2.0*matches.distanceGood / (sqr( matches.distanceGood) + sqr(matches.distanceBad));
		double mu_K = +2.0*matches.distanceBad / (sqr( matches.distanceGood) + sqr(matches.distanceBad));

		G2mLvqPrototype *J = const_cast<G2mLvqPrototype *>(matches.good);
		G2mLvqPrototype *K = const_cast<G2mLvqPrototype *>(matches.bad);
		assert(J == matches.good && K == matches.bad);
		
		TVectorInM vJ(m_vJ.data(),m_vJ.size());
		TVectorInM vK(m_vK.data(),m_vK.size());

		vJ = J->point - trainPoint;
		vK = K->point - trainPoint;
		TVectorOut P_vJ= J->P_point - projectedTrainPoint;
		TVectorOut P_vK = K->P_point - projectedTrainPoint;

		TVectorOut muK2_Bj_P_vJ, muJ2_Bk_P_vK,muK2_BjT_Bj_P_vJ,muJ2_BkT_Bk_P_vK;

	#if EIGEN3
		muK2_Bj_P_vJ.noalias() = (mu_K * 2.0) *  (J->B * P_vJ) ;
		muJ2_Bk_P_vK.noalias() = (mu_J * 2.0) *  (K->B * P_vK) ;
		muK2_BjT_Bj_P_vJ.noalias() =  J->B.transpose() * muK2_Bj_P_vJ ;
		muJ2_BkT_Bk_P_vK.noalias() = K->B.transpose() * muJ2_Bk_P_vK ;
		J->B.noalias() -= lr_B * muK2_Bj_P_vJ * P_vJ.transpose() ;
		K->B.noalias() -= lr_B * muJ2_Bk_P_vK * P_vK.transpose() ;
		J->point.noalias() -=  P.transpose() * (lr_point * muK2_BjT_Bj_P_vJ) ;
		K->point.noalias() -=   P.transpose() * (lr_point * muJ2_BkT_Bk_P_vK) ;
		P.noalias() -= (lr_P * muK2_BjT_Bj_P_vJ) * vJ.transpose() + (lr_P * muJ2_BkT_Bk_P_vK) * vK.transpose() ;
		//	double pNormScale =1.0 /  (P.transpose() * P).diagonal().sum();
		//	P *= pNormScale;
	#else
		muK2_Bj_P_vJ = mu_K * 2.0 * ( J->B * P_vJ ).lazy();
		muJ2_Bk_P_vK = mu_J * 2.0 * ( K->B * P_vK ).lazy();
		muK2_BjT_Bj_P_vJ =  (J->B.transpose() * muK2_Bj_P_vJ).lazy();
		muJ2_BkT_Bk_P_vK = (K->B.transpose() * muJ2_Bk_P_vK).lazy();
		Matrix2d dQdBj = (muK2_Bj_P_vJ * P_vJ.transpose()).lazy();
		Matrix2d dQdBk = (muJ2_Bk_P_vK * P_vK.transpose()).lazy();
		J->B -= lr_B * dQdBj;
		K->B -= lr_B * dQdBk;
		J->point -= (P.transpose() * (lr_point * muK2_BjT_Bj_P_vJ )).lazy();
		K->point -= (P.transpose() * (lr_point * muJ2_BkT_Bk_P_vK )).lazy();
		P -=  ((lr_P * muK2_BjT_Bj_P_vJ) * vJ.transpose()).lazy() + ((lr_P * muJ2_BkT_Bk_P_vK) * vK.transpose()).lazy();
	    //double pNormScale =1.0 /  (P.transpose() * P).lazy().diagonal().sum();
		//	P *= pNormScale;
	#endif

		for(int i=0;i<prototype.size();++i)
			prototype[i].ComputePP(P);
	}
	void learnFrom(VectorXd const & trainPoint, int trainLabel) { learnFrom(TVectorIn(trainPoint),trainLabel);}

	void ClassBoundaryDiagram(double x0, double x1, double y0, double y1, MatrixXi & classDiagram) const {
		int cols = classDiagram.cols();
		int rows = classDiagram.rows();
		for(int xCol=0;  xCol < cols;  xCol++) {
			double x = x0 + (x1-x0) * (xCol+0.5) / cols;
			for(int yRow=0;  yRow < rows;  yRow++) {
				double y = y0+(y1-y0) * (yRow+0.5) / rows;
				TVectorOut vec(x,y);//TODO deal with non 2d case.
				classDiagram(yRow, xCol) = classifyProjectedInternal(vec);
			}
		}
	}

	AbstractLvqModel* clone() { return new G2mLvqModel(*this); }

	size_t MemAllocEstimate() const {
		return 
			sizeof(G2mLvqModel) +
			sizeof(double) * P.size() +
			sizeof(double) * (m_vJ.size() +m_vK.size()) + //various temps
			sizeof(G2mLvqPrototype)*prototype.size() + //prototypes; part statically allocated
			sizeof(double) * (prototype.size() * m_vJ.size()) + //prototypes; part dynamically allocated
			(16/2) * (4+prototype.size()*2);//estimate for alignment mucking.
	}
};








