#include "StdAfx.h"
//#include "SimpleMatrix.h"
//
//
//SimpleMatrix::SimpleMatrix(int withRows, int withCols)
//	: rows(withRows), cols(withCols), underlying_data(new double[withRows*withCols])
//{}
//
//
//SimpleMatrix::~SimpleMatrix(void){}
//
//void SimpleMatrix::clear(void) {
//	for(int i=0;i<rows*cols;i++) underlying_data[i] =0.0;
//}
//
//void SimpleMatrix::mulInto(SimpleMatrix const & right, SimpleMatrix & sink) const {
//		for(int row=0;row<rows;row++) { // A <- B*A
//			double* bRow = underlying_data.get()+row*rows;
//			for(int col=0;col<cols;col++) {
//				double* aCol = right.get()+col;
//
//				double sum=0.0;
//				for(int k=0;k<dims;k++) {
//					sum+=aCol[k*dims]*bRow[k];
//				}
//				tmp[col+row*dims]=sum;
//			}
//		}
//
// //TODO:
//}
//
