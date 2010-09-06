#pragma once
#include "stdafx.h"

template <typename TPoints>
static Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,1> MeanPoint(Eigen::MatrixBase<TPoints>const & points) {
	return points.rowwise().sum() * (1.0/points.cols());
}

template <typename TPoints>
struct CovarianceImpl {
public:
	typedef Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,1> TPoint;
	typedef Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,TPoints::RowsAtCompileTime> TMatrix;

	//decent baseline.
	inline static TMatrix CovarianceA(Eigen::MatrixBase<TPoints> const & points, TPoint const & mean) {
		return (points.colwise() - mean) *  (points.colwise() - mean).transpose()  * (1.0/(points.cols()-1.0)) ;
	}
	
	//good for fixed matrices matrices on MSC
	inline static TMatrix CovarianceB(Eigen::MatrixBase<TPoints>const & points, TPoint const & mean) {
		TPoint diff = TPoint::Zero(points.rows());
		TMatrix cov = TMatrix::Zero(points.rows(),points.rows());
		typename TMatrix::Index dims = points.rows();
		for(int i=0;i<points.cols();++i) {
			diff.noalias() = points.col(i) - mean;
			cov.noalias() += diff * diff.transpose(); 
		}
		return cov * (1.0/(points.cols()-1.0));
	}

	//good for dynamic matrices
	inline static TMatrix CovarianceC(Eigen::MatrixBase<TPoints>const & points, TPoint const & mean) {
		TMatrix cov = TMatrix::Zero(points.rows(),points.rows());
		typename TPoints::PlainObject meanCentered = points.colwise() - mean;
		cov.template selfadjointView<Eigen::Lower>().rankUpdate(meanCentered,1.0/(points.cols()-1.0));
		cov.template triangularView<Eigen::StrictlyUpper>() = cov.adjoint();
		return cov;
	}
	//good for fixed matrices matrices on GCC
	inline static TMatrix CovarianceD(Eigen::MatrixBase<TPoints>const & points, TPoint const & mean) {
		TPoint diff = TPoint::Zero(points.rows());
		TMatrix cov = TMatrix::Zero(points.rows(),points.rows());
		typename TMatrix::Index dims = points.rows();
		for(int i=0;i<points.cols();++i) {
			diff.noalias() = points.col(i) - mean;
			cov.template triangularView<Eigen::Lower>() += diff * diff.adjoint();
		}
		cov.template triangularView<Eigen::StrictlyUpper>() = cov.adjoint();
		return cov * (1.0/(points.cols()-1.0));
	}
};

struct Covariance {


	template<typename TPoints>
	static inline Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,TPoints::RowsAtCompileTime> Compute(Eigen::MatrixBase<TPoints> const & points, Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,1>  const & mean) {
		if(TPoints::RowsAtCompileTime == Eigen::Dynamic)
			return CovarianceImpl<TPoints>::CovarianceC(points,mean);
		else
#ifdef _MSC_VER
			return CovarianceImpl<TPoints>::CovarianceB(points,mean);
#else
			return CovarianceImpl<TPoints>::CovarianceD(points,mean);
#endif
	}

	//template<>
	//static inline Eigen::Matrix2d  Compute<PMatrix>(Eigen::MatrixBase<PMatrix> const & points, Eigen::Vector2d const & mean) {
	//	return CovarianceImpl<PMatrix>::CovarianceB (points,mean);
	//}

	template<typename TPoints>
	static inline Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,TPoints::RowsAtCompileTime> ComputeAutoMean(Eigen::MatrixBase<TPoints>const & points) {
		const typename TPoints::PlainObject & evalPoints = points.eval();
		//Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,1>  mean =;
		return Compute<typename TPoints::PlainObject> (evalPoints,  MeanPoint(evalPoints) );
	}

};
