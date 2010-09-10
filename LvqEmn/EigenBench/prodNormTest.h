#pragma once
#include "standard.h"

double prodNormTest() {
	BenchTimer t;
	double sum=0.0;
	for(int bI=0;bI<BENCH_RUNS;bI++) {

		VectorXd a = VectorXd::Random(DIMS);
		Matrix<double,Eigen::Dynamic,Eigen::Dynamic,0,Eigen::Dynamic,Eigen::Dynamic> mat = MatrixXd::Random(DIMS,DIMS);

		const int num_runs = 20000000/DIMS ;// 20000000;
		t.start();
		for (int i=0; i<num_runs; ++i) {
			sum += (mat*a).squaredNorm();
			a(i%DIMS) *= 0.5;
		}
		t.stop();
	}
	cout <<"(" << sum <<") ";
	return t.best();
}
