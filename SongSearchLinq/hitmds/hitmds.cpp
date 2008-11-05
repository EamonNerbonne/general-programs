// This is the main DLL file.

#include "stdafx.h"

#include "hitmds.h"

#define _CRT_RAND_S
#include <stdlib.h>
#include <stdio.h>
#include <math.h>

//EMN adapted:
#define error(X) fprintf(stderr, (X)), /*main_bye(),*/ exit(1)
#define EPS 1e-16
#define irand(X) ((int)((double)(X) * frand()))
/* address matrix components */
#define D(mat,i,j) (*(mat + ( \
	(i<j) ? (j - 1 + ((((pattern_length<<1) - i - 3) * i) >> 1)) \
		  : ((i==j) ? matsize \
					: (i - 1 + ((((pattern_length<<1) - j - 3) * j) >> 1))) \
	)))
//#define START_ANNEALING_RATIO .5


namespace hitmds {

	/* Euclidean distance */
	inline double dist(int dimension, double *d1, double *d2) 
	{

		int u;
		double sum = 0., tmp;

		for(u = 0; u < dimension; u++) {
			tmp = *d1++ - *d2++;
			sum += tmp * tmp;
		}

		return sqrt(sum);
	}

#define Point(elem) (points+target_dim*(elem))

	Hitmds::Hitmds(int numberOfPoints, int numberOfDimensions, Func<int,int,float> ^distanceLookupFunction,Random^ r) {
		pattern_length=numberOfPoints;
		target_dim = numberOfDimensions;
		matsize = ((numberOfPoints-1) * numberOfPoints) >> 1;
		this->r=r;
		shuffle_index = new int[pattern_length];
		for(int i=0;i<pattern_length;i++)shuffle_index[i]=i;
		nextShuffle = pattern_length;

		pattern_distmat = new FLT[matsize+1];
		points_distmat = new FLT[matsize+1];
		points = new double[pattern_length*target_dim];


		for(int elem=0;elem<pattern_length;elem++) //randomize points
			for(int dim=0;dim<target_dim;dim++)
				Point(elem)[dim] = 2*frand()-1;

		D(points_distmat,0,0) = D(pattern_distmat,0,0) = 0.0f;
		for(int elemA=0;elemA<pattern_length;elemA++)
			for(int elemB=elemA+1;elemB<pattern_length;elemB++) {
				D(points_distmat,elemA,elemB) = (FLT)dist(target_dim,Point(elemA),Point(elemB));
				D(pattern_distmat,elemA,elemB) = (FLT)distanceLookupFunction->Invoke(elemA,elemB);

			}
			data_init();
			//initDistanceMatrices(distanceLookupFunction);
	}

	Hitmds::~Hitmds() {
		delete[] shuffle_index;
		delete[] pattern_distmat;
		delete[] points_distmat;
		delete[] points;
	}




	//Eamon: my usage will be:
	//set pattern_length, patttern_dimension
	//data_alloc()
	//then alloc points (see ABC)
	//init all dims of points randomly in range [-1..1]
	//set D(pattern_mat,i,j) matrix using real distances
	//calc D(point_mat) for random initialization and other statistics (see data_init)
	//





	double Hitmds::frand(void) {
		return r->NextDouble();//TODO remove.
	}

	inline double meanf(int dimension, FLT *p) 
	{

		int i;
		double sum = 0.;

		for(i = 0; i < dimension; i++) sum += *p++;

		return sum / dimension;
	}




	static int stop_calculation = 0;
	double (*distance)(int dimension, double *d1, double *d2) =dist;

	double Hitmds::GetPoint(int point, int dim){return Point(point)[dim];}

	void Hitmds::data_init(void)
	{
		/* initial values of point distance matrix, mean, mixed, mono */
		points_distmat_mean = meanf(matsize, points_distmat);

		/* mean value of pattern distance matrix will be subtracted */
		pattern_distmat_mean = meanf(matsize, pattern_distmat);

		points_distmat_mixed = points_distmat_mono = pattern_distmat_var_sum = 0.;

		for(int i = 0; i < matsize; i++) {
			double tmp = points_distmat[i] - points_distmat_mean;
			pattern_distmat[i] = (FLT)(pattern_distmat[i] - pattern_distmat_mean);
			points_distmat_mixed += tmp * pattern_distmat[i];
			points_distmat_mono +=  tmp * tmp;
			pattern_distmat_var_sum += pattern_distmat[i] * pattern_distmat[i];
		}



	}



	int Hitmds::shuffle_next(void)
	{
		if(++nextShuffle >= pattern_length) {
			for(int cnt = 0; cnt < pattern_length; cnt++) {
				int ind = r->Next(pattern_length);
				int tmp = shuffle_index[cnt];
				shuffle_index[cnt] = shuffle_index[ind];
				shuffle_index[ind] = tmp;
			}
			nextShuffle = 0;
		}
		return shuffle_index[nextShuffle];
	}



	/* for distance matrix discrepancy evaluation. Min val: 0 */
	double Hitmds::corr_2(void)
	{

		double p2 = points_distmat_mixed * points_distmat_mixed,
			e2 = EPS * EPS;

		return pattern_distmat_var_sum*points_distmat_mono/((p2<e2)? e2 : p2) - 1.;
	}




	/* the training loop */
	void Hitmds::mds_train(int cycles, double learning_rate, double start_annealing_ratio, Action<int,int,Hitmds^>^ progressReport)
	{

		double *delta_point, *point, dtmp, 
			diff, diff_mixed, diff_mono, 
			*diffs, diffs_mean;

		FLT *ptmp;

		int c, k, i, j, m, t;
		try{
			delta_point = new double[target_dim];
			diffs = new double[pattern_length];


			t =0, m = cycles / 10;

			for(c = 0; c < cycles && !stop_calculation; c++) {
				progressReport->Invoke(c,cycles,this);
				if(++t == m) {
					t = 0;
					fprintf(stderr, "%3.2f%%: %g  \t(r = %g)\n", 
						100. * c/cycles, corr_2(), 
						1./sqrt(corr_2()+1.));
				}


				i = shuffle_next();

				point = Point(i);


				for(k = 0; k < target_dim; k++) delta_point[k] = 0.;

				for(j = 0; j < pattern_length; j++) {

					if(j != i) {
						double d = D(points_distmat,i, j);
						double  D = D(pattern_distmat,i,j),
				dif = d - points_distmat_mean;
						double preres= (dif * points_distmat_mixed - D * points_distmat_mono)/ ((d < EPS) ? EPS : d);


						for(k = 0; k < target_dim; k++) {

							delta_point[k] += preres * (Point(i)[k] - Point(j)[k]) ;

						} 
					}
				}


				if((diff_mixed = start_annealing_ratio * cycles - c) < 0.)  {
					diff_mixed = learning_rate * (1. + 1. / (1.-start_annealing_ratio) * diff_mixed / cycles);
				}else
					diff_mixed = learning_rate;
				
				//*				
				//this is eamon's alternative point update
				double lenSqr=0;
				for(k = 0; k < target_dim; k++) {
					lenSqr += delta_point[k]*delta_point[k];
				}
				double invLen = diff_mixed *1.0/sqrt(lenSqr) *sqrt((double)target_dim);
				double rndScale = diff_mixed/2;//*(cycles - c)/cycles; //  /2 managed 0.19, cycles-c/cycles...
				for(k = 0; k < target_dim; k++) {
					delta_point[k] = point[k] - delta_point[k]*invLen +  rndScale *(frand()-0.5);
				} 
				/*/
				//the original hitmds update

				for(k = 0; k < target_dim; k++) {
					diff = diff_mixed * (delta_point[k] >0 ? 1 : delta_point[k] <0? -1:0);
					delta_point[k] = point[k] - diff ;  //* (2. * frand() - .5) ;
				}
				/**/



				/* track change of mean, mixed, mono { */
				diff = 0.;  

				for(k = 0; k < pattern_length; k++) {
					if(k != i) {
						dtmp = dist(target_dim, delta_point, Point(k));
						diff += (diffs[k] = dtmp - D(points_distmat, i, k));
					}
				}
				diffs_mean = diff / matsize;

				dtmp = -diffs_mean - points_distmat_mean;

				diff_mixed = diff_mono = 0.;

				for(k = 0; k < pattern_length; k++) {
					if(k != i) {

						ptmp = &D(points_distmat, i, k);

						diff_mixed += D(pattern_distmat, i, k) * diffs[k];

						diff_mono += diffs[k] * (diffs[k] + 2. * (*ptmp + dtmp));

						*ptmp = (FLT)(*ptmp+diffs[k]); /* ugly */
					}
				}
				points_distmat_mean += diffs_mean;

				points_distmat_mixed += diff_mixed; 

				points_distmat_mono += diff_mono + matsize * diffs_mean * diffs_mean;

				/* } track changes of mean, mixed, mono */

				for(k = 0; k < target_dim; k++) 
					point[k] = delta_point[k];



			} /* for cycles */

		} finally {

			delete[] diffs;
			delete[] delta_point;
		}
	}
}