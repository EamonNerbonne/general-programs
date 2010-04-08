#pragma once

#define MAKE_NOOP_CONVERSION(T) \
		 inline T cliToCpp(T  val) {return val;} \
		inline T cppToCli(T const & val) {return val;} 


namespace LvqLibCli {
	//template<typename TIn, typename TOut> TOut cliToCpp(TIn arr);
	//template<typename TIn, typename TOut> TOut cppToCli(TIn const & arr);


	MAKE_NOOP_CONVERSION(int)
	MAKE_NOOP_CONVERSION(unsigned int)
	MAKE_NOOP_CONVERSION(short)
	MAKE_NOOP_CONVERSION(unsigned short)
	MAKE_NOOP_CONVERSION(long long)
	MAKE_NOOP_CONVERSION(unsigned long long)
	MAKE_NOOP_CONVERSION(char)
	MAKE_NOOP_CONVERSION(unsigned char)
	MAKE_NOOP_CONVERSION(float)
	MAKE_NOOP_CONVERSION(double)
	
	template<typename T>
	inline std::vector<T>  cliToCpp(array<T>^ arr) {
		std::vector<T> retval(arr->Length);
		for(int i=0;i<arr->Length;++i)
			retval[i]=cliToCpp(arr[i]);
		return retval;
	}

	template<typename T> 
	inline array<T>^ cppToCli(std::vector<T> const & vec) {
		array<T>^ arr = gcnew array<T>((int)vec.size());
		for(int i=0;i<vec.size();++i)
			arr[i]=cppToCli(vec[i]);
		return arr;
	}

	template <typename T>
	inline Eigen::Matrix<T,Eigen::Dynamic,Eigen::Dynamic> cliToCpp(array<T,2>^ matrix) {
		Matrix<T,Eigen::Dynamic,Eigen::Dynamic> retval(matrix->GetLength(1), matrix->GetLength(0));
		for(int i=0; i < matrix->GetLength(0); ++i)
			for(int j=0; j < matrix->GetLength(1); ++j)
				retval(j,i) = cliToCpp(matrix[i, j]);
		return retval;
	}

	template<typename TDerived> struct cppToCli_MatrixOrVectorChooser {
		template<bool isVector >
		static inline array<typename MatrixBase<TDerived>::Scalar, (isVector?1: 2) >^ cppToCliHelper(MatrixBase<TDerived>  const & matrix);

		template<>
		static inline array<typename MatrixBase<TDerived>::Scalar, 1>^ cppToCliHelper<true>(MatrixBase<TDerived>  const & matrix){
	//			points = gcnew array<T,(MatrixBase<TDerived>::IsVectorAtCompileTime?1: 2)>(matrix.size());
			typedef MatrixBase<TDerived>::Scalar T;
			array<T>^ points =  gcnew array<T>(matrix.size());
			for(int i=0; i<points->GetLength(0); ++i)
				points[i] = cppToCli(matrix(i));
			return points;

		}

		template<>
		static inline array<typename MatrixBase<TDerived>::Scalar, 2>^ cppToCliHelper<false>(MatrixBase<TDerived>  const & matrix) {
			typedef MatrixBase<TDerived>::Scalar T;
			array<T, 2>^ points =  gcnew array<T,2>(matrix.cols(),matrix.rows());
			for(int i=0; i<points->GetLength(0); ++i)
				for(int j=0; j<points->GetLength(1); ++j)
					points[i, j] = cppToCli(matrix(j,i));
			return points;
		}
	};

	template<typename TDerived>
	inline array<typename MatrixBase<TDerived>::Scalar,(MatrixBase<TDerived>::Base::IsVectorAtCompileTime?1: 2) >^ cppToCli(MatrixBase<TDerived>  const & matrix) {
		return cppToCli_MatrixOrVectorChooser<TDerived>::cppToCliHelper<MatrixBase<TDerived>::IsVectorAtCompileTime>(matrix);
	}
}