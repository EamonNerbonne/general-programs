#pragma once
#include "standard.h"

double subtractTest() {
    BenchTimer t;
    double sum = 0.0;
    for (int bI = 0;bI < BENCH_RUNS;bI++) {

        VectorXd a = VectorXd::Random(DIMS);
        VectorXd b = VectorXd::Random(DIMS);
        VectorXd c = VectorXd::Random(DIMS);

        const int num_runs = 100000000; //30000000 was what I used for the forum threads.
        t.start();
        for (int i = 0; i < num_runs; ++i) {
            if (i % 2 == 0)
                c = a - b;
            else
                b = a - c;
        }
        t.stop();
        sum += c.sum();
    }
    cout << "(" << sum << ") ";
    return t.best();
}
