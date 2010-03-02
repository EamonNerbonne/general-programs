#pragma once
#include "standard.h"

double diagTestIter(
	const Vector2d& mu_vJ, const Vector2d& mu_vK,
	const VectorXd& vJ, const VectorXd& vK,
	const double lr_P,
	Matrix<double,2,Dynamic>& P) {
#if EIGEN3
		//Vector2d lrmu_vJ  =lr_P * mu_vJ;
		//Vector2d lrmu_vK =lr_P * mu_vK;
		//P.noalias() -= lrmu_vJ.eval() * vJ.transpose();
		//P.noalias() -= lrmu_vK.eval() * vK.transpose();

		//P.noalias() -= (lr_P * mu_vJ).eval() * vJ.transpose();
		//P.noalias() -= (lr_P * mu_vK).eval() * vK.transpose();
		P.noalias() -= lr_P * ( mu_vJ * vJ.transpose() + mu_vK * vK.transpose());
		return 1.0 / ( (P.transpose() * P).diagonal().sum());
#else
		P = P-  lr_P * (( mu_vJ * vJ.transpose()).lazy() +( mu_vK * vK.transpose()).lazy() );
		return 1.0 /  (P.transpose() * P).lazy().diagonal().sum();
#endif
}

double diagTest() {
	BenchTimer t;
	double sum = 0.0;
	for(int bI=0;bI<BENCH_RUNS;bI++) {

		Vector2d mu_vJ = Vector2d::Random();
		Vector2d mu_vK = Vector2d::Random();
		VectorXd vJ = VectorXd::Random(DIMS);
		VectorXd vK = VectorXd::Random(DIMS);
		Matrix<double,2,Dynamic> P = Matrix<double,2,Dynamic>::Random(2,DIMS);
		double lr_P = ei_random<double>();

		const int num_runs = 5000000; //5 000 000 was what I used for the forum threads.
		t.start();
		for (int i=0; i<num_runs; ++i) {
			P(num_runs%2, (num_runs/2)%DIMS) = 1.0;
			sum += diagTestIter(mu_vJ, mu_vK, vJ, vK, lr_P, P);
		}
		t.stop();
	}
	cout <<"(" << sum<<") ";
	return t.best();
}
