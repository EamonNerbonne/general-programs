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

	inline static TMatrix CovarianceA(Eigen::MatrixBase<TPoints>const & points) {
		return CovarianceA(points,MeanPoint(points));
	}
	inline static TMatrix CovarianceA(Eigen::MatrixBase<TPoints> const & points, TPoint const & mean) {
		return (points.colwise() - mean) *  (points.colwise() - mean).transpose()  *(1.0/(points.cols()-1.0)) ;
	}


	//equiv possibly faster version:
	inline static TMatrix CovarianceB(Eigen::MatrixBase<TPoints>const & points) {
		return CovarianceB(points,MeanPoint(points));
	}
	inline static TMatrix CovarianceB(Eigen::MatrixBase<TPoints>const & points, TPoint const & mean) {
		TPoint diff = TPoint::Zero(points.rows());
		TMatrix cov = TMatrix::Zero(points.rows(),points.rows());
		for(int i=0;i<points.cols();++i) {
			diff.noalias() = points.col(i) - mean;
			cov.noalias() += diff * diff.transpose();
		}
		return cov * (1.0/(points.cols()-1.0));
	}
};

struct Covariance {

	template<typename TPoints>
	static inline Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,TPoints::RowsAtCompileTime> Compute(Eigen::MatrixBase<TPoints>const & points) {
		return CovarianceImpl<TPoints>::CovarianceA (points);
	}
	template<typename TPoints>
	static inline Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,TPoints::RowsAtCompileTime> Compute(Eigen::MatrixBase<TPoints> const & points, Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,1> const & mean) {
		return CovarianceImpl<TPoints>::CovarianceA (points,mean);
	}

	static inline Eigen::Matrix2d Compute(Eigen::MatrixBase<PMatrix>const & points) {
		return CovarianceImpl<PMatrix>::CovarianceB (points);
	}
	static inline Eigen::Matrix2d  Compute(Eigen::MatrixBase<PMatrix> const & points, Eigen::Vector2d const & mean) {
		return CovarianceImpl<PMatrix>::CovarianceB (points,mean);
	}
};
