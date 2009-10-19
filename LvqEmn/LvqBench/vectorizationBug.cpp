#include "stdafx.h"
#include "vectorizationBug.h"
USING_PART_OF_NAMESPACE_EIGEN

void VecTest() {
		using namespace std;
		using boost::scoped_ptr;
		
		Matrix2d A(Matrix2d::Identity());//value irrelevant

		Vector2d v;
		v<< 3,5;//irrelevant
		
		cout<<"Trying:  A^T * v\n";
		Vector2d w1 = A.transpose() * v; //fails in x64 with vectorization, succeeds in x64 without vectorization, succeeds in x86 with or without vectorization

		cout<< w1 << endl;
		cout<<"END-TEST\n\n\n";
	}
