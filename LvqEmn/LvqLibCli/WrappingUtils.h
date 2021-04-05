#pragma once
#include <msclr/marshal_cppstd.h>

//#pragma managed(push, off)
#include <Eigen/Core>
//#pragma managed(pop)

namespace LvqLibCli {

	value class LvqTrainingStatCli;

	generic<typename T>
	public value class MatrixContainer {//rowmajor
	public:
		array<T>^ arr;
		int cols;
		int rows;

		//property int rows {int get(){return arr->Length / cols;}}
		bool IsSet() {return arr != nullptr;}
		void Set(int row, int col,T val) {arr[row*cols+col]=val; }
		T Get(int row, int col) {return arr[row*cols+col];}
		property T default[int,int] {
			T get(int y,int x) { return arr[y*cols+x]; }
			void set(int y,int x,T val) { arr[y*cols+x] = val; }
		}

		MatrixContainer(int rows, int cols) : cols(cols),rows(rows) {arr=gcnew array<T>(rows*cols); }
	};

	using namespace System;
	using namespace Eigen;
	using std::wstring;
	using std::vector;

#define MAKE_NOOP_CONVERSION(T) \
	inline void cliToCpp(T  val, T& retval) {retval=val;} \
	inline void cppToCli(T const & val, T%retval) {retval= val;} 

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


		template<typename T,typename S>
	inline void cliToCpp(array<T>^ arr, vector<S> &vec) {
		vec = vector<S>(arr->Length);
		for(int i=0;i<arr->Length;++i)
			cliToCpp(arr[i], vec[i]);
	}

	template<typename T,typename S> 
	inline void cppToCli(std::vector<T> const & vec, array<S>^% arr) {
		arr = gcnew array<S>((int)vec.size());
		for(unsigned i=0;i<vec.size();++i)
			cppToCli(vec[i], arr[i]);
	}

	inline void cppToCli(wstring const & src, String^% dst) {
		dst = gcnew String(src.c_str());
	}

	inline void cliToCpp(String^ src, wstring & dst) {
		dst = msclr::interop::marshal_as<wstring>(src);
	}


	template<typename TDerived> struct MatrixOrVectorChooser {
		template<bool isVector >
		static inline array<typename MatrixBase<TDerived>::Scalar, (isVector?1: 2) >^ cppToCliHelper(MatrixBase<TDerived> const & matrix);
		template<>
		static inline array<typename MatrixBase<TDerived>::Scalar>^ cppToCliHelper<true>(MatrixBase<TDerived> const & matrix){
			typedef MatrixBase<TDerived>::Scalar T;
			array<T>^ points = gcnew array<T>(static_cast<int>(matrix.size()));
			for(int i=0; i<points->GetLength(0); ++i)
				cppToCli(matrix(i),points[i]);
			return points;
		}
		template<>
		static inline array<typename MatrixBase<TDerived>::Scalar, 2>^ cppToCliHelper<false>(MatrixBase<TDerived> const & matrix) {
			typedef MatrixBase<TDerived>::Scalar T;
			array<T, 2>^ points = gcnew array<T,2>(static_cast<int>(matrix.cols()),static_cast<int>(matrix.rows()));
			for(int i=0; i<points->GetLength(0); ++i)
				for(int j=0; j<points->GetLength(1); ++j)
					cppToCli(matrix(j,i),points[i, j]);
			return points;
		}

		template<bool isVector >
		static inline TDerived cliToCppHelper(array<typename MatrixBase<TDerived>::Scalar, (isVector?1: 2)>^ cliarr);
		template<>
		static inline TDerived cliToCppHelper<true>(array<typename MatrixBase<TDerived>::Scalar, 1>^ cliarr){
			TDerived retval(cliarr->Length);
			for(int i=0; i < cliarr->Length; ++i)
				cliToCpp(cliarr[i],retval(i));
			return retval;
		}
		template<>
		static inline TDerived cliToCppHelper<false>(array<typename MatrixBase<TDerived>::Scalar, 2>^ cliarr){
			TDerived retval(cliarr->GetLength(1), cliarr->GetLength(0));
			for(int i=0; i < cliarr->GetLength(0); ++i)
				for(int j=0; j < cliarr->GetLength(1); ++j)
					cliToCpp(cliarr[i, j],retval(j,i));
			return retval;
		}

	};

	template<typename TDerived>
	inline void cppToCli(MatrixBase<TDerived> const & matrix,array<typename MatrixBase<TDerived>::Scalar, (MatrixBase<TDerived>::Base::IsVectorAtCompileTime?1: 2) >^% retval ) {
		retval= MatrixOrVectorChooser<TDerived>::cppToCliHelper<MatrixBase<TDerived>::IsVectorAtCompileTime>(matrix);
	}

	template<typename TDerived>
	inline void cppToCli(MatrixBase<TDerived> const & matrix, MatrixContainer<typename MatrixBase<TDerived>::Scalar>%points) {

		typedef MatrixBase<TDerived>::Scalar T;
		points=MatrixContainer<T>(static_cast<int>(matrix.cols()), static_cast<int>(matrix.rows()));

		for(int i=0; i<points.rows; ++i)
			for(int j=0; j<points.cols; ++j) {
				T val;
				cppToCli(matrix(j,i), val);
				points.Set(i, j, val);
			}
	}



	template<typename TDerived>
	inline void cppToCli(MatrixBase<TDerived> const & matrix,System::Windows::Point% retval ) {
		assert(matrix.rows()==2);
		assert(matrix.cols()==1);
		retval = System::Windows::Point(matrix(0,0),matrix(1,0));
		//		retval= MatrixOrVectorChooser<TDerived>::cppToCliHelper<MatrixBase<TDerived>::IsVectorAtCompileTime>(matrix);
	}

	template<typename TDerived>
	inline void cliToCpp(array<typename MatrixBase<TDerived>::Scalar, (MatrixBase<TDerived>::Base::IsVectorAtCompileTime?1: 2) >^ cliarr, MatrixBase<TDerived> & retval) {
		retval= MatrixOrVectorChooser<TDerived>::cliToCppHelper<MatrixBase<TDerived>::IsVectorAtCompileTime>(cliarr);
	}

	template<typename S>
	struct ToCli {
		template<typename T> static S From(T% cliVal) {
			S retval;
			cppToCli(cliVal,retval);
			return retval;
		}
	};

	template<typename S>
	struct ToCpp {
		template<typename T> static S From(T const & cppVal) {
			S retval;
			cliToCpp(cppVal,retval);
			return retval;
		}
	};
}

