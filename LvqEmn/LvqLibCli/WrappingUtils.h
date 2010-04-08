#pragma once


namespace LVQCppCli {
	template<typename T>
	std::vector<T>  cliToCpp(array<T>^ arr) {
		std::vector<T> retval(arr->Length);
		for(int i=0;i<arr->Length;++i)
			retval[i]=arr[i];
		return retval;
	}

	template<typename T>
	array<T>^ cppToCli(std::vector<T> const & vec ) {
		array<T>^ arr = gcnew array<T>((int)vec.size());
		for(int i=0;i<vec.size();++i)
			arr[i]=vec[i];
		return arr;
	}

	template <typename T>
	Eigen::Matrix<T,Eigen::Dynamic,Eigen::Dynamic> cliToCpp(array<T,2>^ matrix) {
		Matrix<T,Eigen::Dynamic,Eigen::Dynamic> retval(matrix->GetLength(1), matrix->GetLength(0));
		for(int i=0; i < matrix->GetLength(0); ++i)
			for(int j=0; j < matrix->GetLength(1); ++j)
				retval(j,i) = matrix[i, j];
		return retval;
	}

	template<typename TDerived>
	array<typename MatrixBase<TDerived>::Scalar, 2>^ cppToCli(MatrixBase<TDerived>  const & matrix) {
		typedef MatrixBase<TDerived>::Scalar T;
		array<T, 2>^ points = gcnew array<T,2>(matrix.cols(),matrix.rows());
		for(int i=0; i<points->GetLength(0); ++i)
			for(int j=0; j<points->GetLength(1); ++j)
				points[i, j] = matrix(j,i);

		return points;
	}
}