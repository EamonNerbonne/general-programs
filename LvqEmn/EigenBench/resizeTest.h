#pragma once
#include "standard.h"

#define MAXDIMS 16
#define ACTUALDIMS 7
typedef Matrix<double,Dynamic,1,0,MAXDIMS,1>  VectorLimited;
#define NO_MAP 0

double resizeTest() {
	BenchTimer t;
	double sum=0.0;
	cerr <<"{sizeof(VectorLimited)=="<<sizeof(VectorLimited)<<"}";
	for(int bI=0;bI<BENCH_RUNS;bI++) {
#if NO_MAP
		VectorXd v;
#else
		double* mem=ei_aligned_new<double>(MAXDIMS);
		Map<VectorXd,Aligned> v2(mem,ACTUALDIMS);
#endif

		t.start();
		for(int j=0;j<10000000;j++) {
#if NO_MAP
			v = VectorLimited::Constant(ACTUALDIMS,double(j));
			sum+=v.sum();
#else
			v2=VectorLimited::Constant(ACTUALDIMS,double(j));
			sum+=v2.sum();
#endif
		}
		t.stop();
#if !NO_MAP
		ei_aligned_delete(mem,MAXDIMS);
#endif
	}
	cout <<"(" << sum <<") ";
	return t.best();
}
