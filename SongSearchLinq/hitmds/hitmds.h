// hitmds.h

#pragma once

using namespace System;

namespace hitmds {
	typedef float FLT;

	public ref class Hitmds
	{
		// TODO: Add your methods for this class here.

	public:
		Hitmds(int numberOfPoints, int numberOfDimensions, Func<int,int,float> ^distanceLookupFunction);
		~Hitmds();
		void mds_train(int cycles, double learning_rate, Action<int,int>^ progressReport);
		double GetPoint(int point,int dim);
	private:
		Random ^r;
		//from header:
	int correlation_exponent,/**/  /* (1/r^2)^k */
		pattern_length,      /*s*/  /* Length of data series */
		matsize,             /*s*/  /* number of distance matrix elements */
		target_dim;          /*s*/  /* dimension of target space */

	double 	*points,		/*a*/  
		points_distmat_mean,	/**/  
		points_distmat_mixed,	/**/  
		points_distmat_mono,	/**/  
		pattern_distmat_var_sum,/**/  
		pattern_distmat_mean//,	/**/  
		//correps
		
		;				/**/  	/* correlation epsilon avoiding 1/0 */

	FLT  *pattern_distmat,
			*points_distmat;
	

	int*shuffle_index;
	int nextShuffle;

		double frand(void);
		void data_init(void);
		int shuffle_next(void);
		double corr_2(void);
		double deriv(int j, int k, int idx);
	};
}
