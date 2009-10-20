// EigenTest.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <iostream>
#include <Eigen/Core>
#include <boost/smart_ptr/scoped_ptr.hpp>


USING_PART_OF_NAMESPACE_EIGEN

struct BuggyAlignment {
		int ignore;
		Matrix2d mat;
		char ignore2;
		char ignore5;
		BuggyAlignment(Matrix2d const & initializer) : mat(initializer){}
};


int _tmain(int argc, _TCHAR* argv[])
{
		using namespace std;
		using boost::scoped_ptr;

#ifndef NDEBUG
		cout << "DEBUG mode!\n";
#endif
		
		Matrix2d A(Matrix2d::Identity());//value irrelevant

		Vector2d v;
		v<< 3, 5;//irrelevant
		
        cout <<"align(A):"<< long long(&A) %16<<endl; //0

		cout<<"Trying:  A^T * v\n";
		Vector2d w1 = (A.transpose()).eval() * v; //fails in x64 with vectorization, succeeds in x64 without vectorization, succeeds in x86 with or without vectorization

		cout<< w1 << endl;
		cout<<"END-TEST\n\n\n";

	return 0;
}

