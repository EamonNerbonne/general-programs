#pragma once
#include "standard.h"

EIGEN_DONT_INLINE
double projectionTestIter(
		const VectorXd& point, 
		Matrix<double,2,Dynamic>& P) {
#if EIGEN3
	Vector2d P_point = P*point;
#else
	Vector2d P_point = (P*point).lazy();
#endif
	return P_point.sum();
}

void projectionTest() {
	VectorXd a = VectorXd::Random(DIMS);
	VectorXd b = VectorXd::Random(DIMS);
	Matrix<double,2,Dynamic> P = Matrix<double,2,Dynamic>::Random(2,DIMS);

	progress_timer t(cerr);
	double sum = 0.0;
	const int num_runs = 30000000; //30000000 was what I used for the forum threads.
	for (int i=0; i<num_runs; ++i) {
		if(num_runs % (i+1) > sum)
			sum -= projectionTestIter(a, P);
		else
			sum += projectionTestIter(b, P);
	}
	cout <<"(" << sum<<") ";
}
