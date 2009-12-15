#pragma once
namespace LVQCppCli {

	template<typename T>
	Matrix<T,Eigen::Dynamic,Eigen::Dynamic> arrayToMatrix(array<T,2>^ points) {
		Matrix<T,Eigen::Dynamic,Eigen::Dynamic> nPoints(points->GetLength(1), points->GetLength(0));
		for(int i=0; i<points->GetLength(0); ++i)
			for(int j=0; j<points->GetLength(1); ++j)
				nPoints(j,i) = points[i, j];

		return nPoints;
	}

	template<typename T, int rowsDEF, int colsDEF>
	array<T,2>^ matrixToArray(Matrix<T,rowsDEF,colsDEF>  const & matrix) {
		array<T,2>^ points = gcnew array<T,2>(matrix.cols(),matrix.rows());
		for(int i=0; i<points->GetLength(0); ++i)
			for(int j=0; j<points->GetLength(1); ++j)
				points[i, j] = matrix(j,i);

		return points;
	}

	template<typename T, int rowsDEF, int colsDEF>
	array<T,2>^ matrixToArrayNOFLIP(Matrix<T,rowsDEF,colsDEF>  const & matrix) {
		array<T,2>^ points = gcnew array<T,2>(matrix.rows(),matrix.cols());
		for(int i=0; i<points->GetLength(0); ++i)
			for(int j=0; j<points->GetLength(1); ++j)
				points[i, j] = matrix(i,j);

		return points;
	}
}