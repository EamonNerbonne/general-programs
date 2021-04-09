#pragma once
#include "standard.h"

EIGEN_DONT_INLINE
double projectionTestIter(
    const VectorXd& point, 
    Matrix<double,2,Dynamic>& P) {
        Vector2d P_point = P*point;
        return P_point.sum();
}

double projectionTest() {
    BenchTimer t;
    double sum = 0.0;
    for(int bI=0;bI<BENCH_RUNS;bI++) {

        VectorXd a = VectorXd::Random(DIMS);
        VectorXd b = VectorXd::Random(DIMS);
        Matrix<double,2,Dynamic> P = Matrix<double,2,Dynamic>::Random(2,DIMS);

        const int num_runs = 30000000; //30000000 was what I used for the forum threads.
        t.start();
        for (int i=0; i<num_runs; ++i) {
            if(num_runs % (i+1) > sum)
                sum -= projectionTestIter(a, P);
            else
                sum += projectionTestIter(b, P);
        }
        t.stop();
    }
    cout <<"(" << sum<<") ";
    return t.best();
}
