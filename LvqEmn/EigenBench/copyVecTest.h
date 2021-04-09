#pragma once
#include "standard.h"
#define VEC_COUNT 10000

double copyVecTest() {
    BenchTimer t;
    double sum=0.0;
    for(int bI=0;bI<BENCH_RUNS;bI++) {

        VectorXd a = VectorXd::Random(DIMS);
        MatrixXd vecs = MatrixXd::Random(DIMS,VEC_COUNT);

        const int num_runs = VEC_COUNT * 50;// 20000000;
        t.start();
        for (int i=0; i<num_runs; ++i) {
            int colI =unsigned (i * 97 + i*i) % VEC_COUNT;
            a = vecs.col(colI);
            sum+=a.sum();
        }
        t.stop();
    }
    cout <<"(" << sum <<") ";
    return t.best();
}
