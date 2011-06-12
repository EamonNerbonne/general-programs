#include <Eigen/Core>
#include <Eigen/EigenValues>
using namespace Eigen;

template <typename TPoints>
struct PCA {
	typedef Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,1> TPoint;
	typedef Eigen::Matrix<typename TPoints::Scalar,TPoints::RowsAtCompileTime,TPoints::RowsAtCompileTime> TMatrix;
	static void DoPcaFromCov(TMatrix const & covarianceMatrix, TMatrix & transform, TPoint & eigenvalues ) {	
		Eigen::SelfAdjointEigenSolver<TMatrix> eigenSolver(covarianceMatrix, Eigen::ComputeEigenvectors);
		TPoint eigenvaluesUnsorted = eigenSolver.eigenvalues();
		TMatrix eigVecUnsorted = eigenSolver.eigenvectors();
		std::vector<int> v;
		for(int i=0;i<eigenvaluesUnsorted.size();++i)
			v.push_back(i);
		std::sort(v.begin(),v.end(), [&eigenvaluesUnsorted](size_t a, size_t b) -> bool { return eigenvaluesUnsorted(a) > eigenvaluesUnsorted(b);});

		assert(eigVecUnsorted.cols() ==eigVecUnsorted.rows());
		transform.resize(eigVecUnsorted.cols(),eigVecUnsorted.rows());
		eigenvalues.resize(eigenvaluesUnsorted.size());

		for(int i=0;i<eigenvalues.size();++i) {
			transform.row(i).noalias() = eigVecUnsorted.col(v[i]);
			eigenvalues(i) = eigenvaluesUnsorted(v[i]);
		}
	}
};

void HighDimPca(MatrixXd const & cov, MatrixXd &trans,VectorXd & vals) {PCA<MatrixXd>::DoPcaFromCov(cov,trans,vals);}
