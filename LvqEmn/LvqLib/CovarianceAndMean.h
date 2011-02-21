#pragma once
#include <Eigen/Core>

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
		return (points.colwise() - mean) * (points.colwise() - mean).transpose() * (1.0/(points.cols()-1.0)) ;
	}

	//good for fixed matrices matrices on MSC
	inline static TMatrix CovarianceB(Eigen::MatrixBase<TPoints>const & points, TPoint const & mean) {
		TPoint diff = TPoint::Zero(points.rows());
		TMatrix cov = TMatrix::Zero(points.rows(),points.rows());
		for(int i=0;i<points.cols();++i) {
			diff.noalias() = points.col(i) - mean;
			cov.noalias() += diff * diff.transpose(); 
		}
		return cov * (1.0/(points.cols()-1.0));
	}

	//good for dynamic matrices (and good for fixed matrices matrices on MSC)
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
	static inline Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,TPoints::RowsAtCompileTime> Compute(Eigen::MatrixBase<TPoints> const & points, Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,1> const & mean) {
#ifdef _MSC_VER
#pragma warning (push, 3)
#pragma warning (disable: 4127)
		if(TPoints::RowsAtCompileTime == Eigen::Dynamic)
			return CovarianceImpl<TPoints>::CovarianceC(points,mean);
		else 
			return CovarianceImpl<TPoints>::CovarianceB(points,mean);
#pragma warning (pop)
#else
		if(TPoints::RowsAtCompileTime == Eigen::Dynamic)
			return CovarianceImpl<TPoints>::CovarianceC(points,mean);
		else
			return CovarianceImpl<TPoints>::CovarianceD(points,mean);
#endif
	}

	template<typename TPoints>
	static inline Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,TPoints::RowsAtCompileTime> ComputeWithMean(Eigen::MatrixBase<TPoints>const & points) {
		const typename TPoints::PlainObject & evalPoints = points.eval();
		//Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,1>  mean =;
		return Compute<typename TPoints::PlainObject> (evalPoints, MeanPoint(evalPoints) );
	}
};
