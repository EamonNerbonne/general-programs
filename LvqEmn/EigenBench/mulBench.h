#pragma once
#include "standard.h"

double mulBench(void) {
    double sum = 0.0;
    BenchTimer t;
    for(int bI=0;bI<BENCH_RUNS;bI++) {
        Vector2d mu_vK = Vector2d::Random();
        VectorXd vK = VectorXd::Random(25);
        Matrix<double,2,Dynamic> P = Matrix<double,2,Dynamic>::Random(2,25);
        const int num_runs = 50000000;

        t.start();
        for (int i=0; i<num_runs; ++i) {
            P(num_runs%2, (num_runs/2)%25) = 1.0; //vs. optimizer
            sum += mu_vK.dot(P * vK);
        }
        t.stop();
    }
    cout << " ("<<sum << ") " ;//vs. optimizer
    return t.best();
}
