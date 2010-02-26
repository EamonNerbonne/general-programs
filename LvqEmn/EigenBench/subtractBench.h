#pragma once
#include "standard.h"
//
//EIGEN_DONT_INLINE
//void subtractTestIter(
//		const VectorXd& pointA, 
//		const VectorXd& pointB, 
//		VectorXd & pointOut) {
////#if EIGEN3
//			pointOut = pointA-pointB;
////#else
////	Vector2d P_point = (P*point).lazy();
////#endif
//}

void subtractTest() {
	VectorXd a = VectorXd::Random(DIMS);
	VectorXd b = VectorXd::Random(DIMS);
	VectorXd c = VectorXd::Random(DIMS);

	progress_timer t(cerr);
	double sum = 0.0;
	const int num_runs = 30000000; //30000000 was what I used for the forum threads.
	for (int i=0; i<num_runs; ++i) {
#if EIGEN3
		if(i%2==0)
			c = a - b;
		else
			b  = a - c;
#else
		if(i%2==0)
			c = a - b;
		else
			b  = a - c;
#endif

	}
	cout <<"(" << c(0) <<") ";
}
