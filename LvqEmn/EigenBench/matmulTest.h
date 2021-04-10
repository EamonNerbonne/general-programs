#pragma once
#include "standard.h"

//#define TRANSPOSED 1
typedef Matrix<double, 2, Dynamic> PMatrix;
typedef Matrix<double, Dynamic, 2> QMatrix;
double matmulTest() {
    BenchTimer t;
    double sum = 0.0;
    for (int bI = 0;bI < BENCH_RUNS;bI++) {
#if TRANSPOSED
        PMatrix P = PMatrix::Random(2, DIMS);
#else
        QMatrix Q = QMatrix::Random(DIMS, 2);
#endif
        Vector2d v = Vector2d::Random();
        VectorXd r = VectorXd::Random(DIMS);

        const int num_runs = 10000000; //30000000 was what I used for the forum threads.
        t.start();
        for (int i = 0; i < num_runs; ++i) {
#if TRANSPOSED
            r.noalias() = P.transpose() * v;
#else
            r.noalias() = Q * v;
#endif
            v(i % 2) = 0.5 * (v(i % 2) + (i % DIMS));
            sum += r.sum() * 0.0000001;
        }
        t.stop();
    }
#if TRANSPOSED
    cout << "t";
#endif
    cout << "(" << sum << ") ";
    return t.best();
}