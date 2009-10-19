#include "stdafx.h"
#include "vectorizationBug.h"
USING_PART_OF_NAMESPACE_EIGEN

//struct TestA {
//	int val;
//	TestA():val(42){}
//};
//
//struct TestB {
//	TestB():ptr(new TestA){}
//	~TestB(){delete ptr;} //don't do no copying, boy...
//	TestA* getPtr() { return ptr;}
//	TestA  * getPtr() const {return ptr;}
//private:
//	TestA *ptr;
//};
//
//void TestIt(TestB const * refB) {
//	refB->getPtr()->val = 43;
//}


struct BuggyAlignment {
		int ignore;
		Matrix2d mat;
		char ignore2;
		char ignore5;
		BuggyAlignment(Matrix2d const & initializer) : mat(initializer){}
};

void VecTest() {
		using namespace std;
		using boost::scoped_ptr;
		
		Matrix2d A(Matrix2d::Identity());//value irrelevant
        scoped_ptr<BuggyAlignment> B(new BuggyAlignment(A));

		Vector2d v;
		v<< 3,5;//irrelevant
		
        cout <<"align(A):"<< long long(&A) %16<<endl; //0
		cout <<"align(B->mat):"<< long long(&(B->mat)) %16<<endl;//0
		cout <<"align(B->mat):"<< long long(&(B->ignore5)) %16<<endl;//1
		cout <<"align(B->ignore5):"<< (void*)B.get()<<", "<< (void*)&B.get()->ignore<<", "<< (void*)&B.get()->mat <<", "<< (void*)&B.get()->ignore2<<", "<< (void*)&B.get()->ignore5  <<endl;//0??

		cout<<"Trying:  A^T * v\n";
		Vector2d w1 = A.transpose() * v; //fails in x64 with vectorization, succeeds in x64 without vectorization, succeeds in x86 with or without vectorization

		cout<< w1 << endl;
		w1 += B->mat * v; //alignement error works fine under debug mode.
		cout<<"END-TEST\n\n\n";
	}
