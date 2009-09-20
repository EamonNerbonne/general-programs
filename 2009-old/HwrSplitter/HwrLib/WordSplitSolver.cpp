#include "StdAfx.h"
#include "WordSplitSolver.h"
#include <boost/timer.hpp>

WordSplitSolver::WordSplitSolver(AllSymbolClasses const & syms, ImageFeatures const & imageFeatures, std::vector<short> const & targetString,std::vector<int> const & overrideEnds, double featureRelevance) 
: syms(syms)
, imageFeatures(imageFeatures)
, imageLen1(imageFeatures.getImageWidth()+1)
, targetString(targetString) 
, overrideEnds(overrideEnds)
, symToStrIdx(new vector<short>[syms.size()])
{
#if LOGLEVEL >=9
	boost::timer overallTimer;
#endif

	//first we initialize a lookup table mapping symbol to the indexes at which it is used in the string.
	init_symToStrIdx();

	//then we make a list of used symbols
	init_usedSymbols();

	//next, we determine the loglikelihood of observing each state at every index.
	init_op_x_u_i(featureRelevance);
	
#if LOGLEVEL >=9
	std::cout << "Init: "<<overallTimer.elapsed();
#endif

	//we marginalize to find the cumulative loglikelihood of observing all pixels between any x0 and x1 for any given symbol.
	init_opC_x_u_i(featureRelevance);

#if LOGLEVEL >=9
	std::cout << ",  Marginalize: "<<overallTimer.elapsed();
#endif

	//then we compute the forward loglikelihood: observing all symbols upto and including u over pixels upto and excluding x
	init_pf_x_u();

	//similarly, we compute the backward loglikelihood: observing all symbols starting with u and later over pixels starting at x until the end of the image.
	init_pb_x_u();

#if LOGLEVEL >=9
	std::cout << ",  fwd/bk: "<<overallTimer.elapsed();
#endif

	//compute the loglikelihood of u starting precisely at x: p(x,u) == pf(x,u-1) + pb(x,u)
	init_p_x_u();

	//compute the cumulative (non-log) likelihood of symbol u starting at or before x
	init_pC_x_u();

	//compute the cumulative (non-log) likelihood of symbol u, subphase i starting at or before x
	init_pCi_x_u_i();

	//then compute the probability of a particular symbol at a particular pixel.
	init_P_x_u();

#if LOGLEVEL >=9
	std::cout << ", final: " << overallTimer.elapsed()<<"\n";
#endif

}




std::vector<int> WordSplitSolver::MostLikelySplit(double & loglikelihood) {
	using namespace std;
	vector<int> splits;
	int x=0;

	double featureLikelihoodSum=0.0;
	double lenLikelihoodSum=0.0;
	for(int u=0; u< (int)strLen()-1;u++) {
		int startX = x;
		while(x <(int) imageLen() && P(x,u) > P(x,u+1)) { 
			x++;
		}
		int len = x - startX;
		for(int i=0;i<SUB_PHASE_COUNT;i++) 
			for(int xp = startX + i*len/SUB_PHASE_COUNT;   xp < startX + (i+1)*len/SUB_PHASE_COUNT;   xp++)
				featureLikelihoodSum += sym(u).phase[i].LogProbDensityOf(imageFeatures.featAt(xp));
		

#if !LENGTH_WEIGHT_ON_TERMINATORS
		if(u>0)
#endif
			lenLikelihoodSum += sym(u).LogLikelihoodLength(len);
		splits.push_back(x);
	}
	
	int U = (int)strLen()-1;
	int startX = x;
	int len =  imageLen() - startX;
	for(int i=0;i<SUB_PHASE_COUNT;i++) 
		for(int xp = startX + i*len/SUB_PHASE_COUNT;   xp < startX + (i+1)*len/SUB_PHASE_COUNT;   xp++)
			featureLikelihoodSum += sym(U).phase[i].LogProbDensityOf(imageFeatures.featAt(xp));

	splits.push_back(imageLen());
#if LENGTH_WEIGHT_ON_TERMINATORS
		lenLikelihoodSum += sym(U).LogLikelihoodLength(len);
#endif


	loglikelihood = featureLikelihoodSum/imageLen() + lenLikelihoodSum/(strLen()
#if !LENGTH_WEIGHT_ON_TERMINATORS
		-2
#endif
		);

	return splits;
}


void WordSplitSolver::Learn(double blurSymbols, AllSymbolClasses& learningTarget){
	using namespace std;
	using namespace boost;

	timer overallTimer;

	//we need to "guess" the sub-state loglikelihoods: we've only the overall probability P(x,u)

	for(int x=0;x<(int)imageLen();x++) {
		FeatureVector const & fv = imageFeatures.featAt(x);
		for(int u=0;u<(int)strLen();u++) {
			int c=targetString[u];
			for(int i=0;i<SUB_PHASE_COUNT;i++) 
				syms[c].phase[i].CombineInto(fv, Pi(x,u,i), learningTarget[c].phase[i]);
		}
	}

#if LOGLEVEL >= 9 
	cout<<"SymbolLearning: "<<overallTimer.elapsed()<<endl;
#endif
	overallTimer.restart();

	//now we'd like to somehow compute the expected length+ variance of each symbol.

#if LENGTH_WEIGHT_ON_TERMINATORS
	vector<double> lengthWeight(imageLen1);
	{
		SymbolClass & sc = learningTarget[targetString[0]];
		SymbolClass const & scOrig =  syms[targetString[0]];
		for(unsigned x1=0;x1<imageLen1;x1++) {
			sc.LearnLength(double(x1), p(0,0) * p(x1,0+1) * exp(scOrig.LogLikelihoodLength(x1-0)) );
		}
    }
#endif

	for(int u=1;u <(int)strLen()-1; u++) {
		SymbolClass & sc =  learningTarget[targetString[u]];
		SymbolClass const & scOrig =  syms[targetString[u]];

		for(unsigned i=0;i<imageLen1;i++) {
			double lenFactor= exp( scOrig.LogLikelihoodLength(i));
			double sum =0.0;
			for(unsigned x0=0;x0<imageLen1-i;x0++) 
				sum+= p(x0,u) * p(x0+i,u+1);
			sc.LearnLength(double(i),sum*lenFactor);
		}
	}
#if LENGTH_WEIGHT_ON_TERMINATORS
	{
		int U = strLen()-1;
		SymbolClass & sc =  learningTarget[targetString[U]];
		SymbolClass const & scOrig =  syms[targetString[U]];
		for(unsigned x0=0;x0<imageLen1;x0++) 
			sc.LearnLength(double(imageLen()-x0), p(x0,U) * exp(scOrig.LogLikelihoodLength(imageLen()-x0) ));
	}
#endif

	if(blurSymbols != 0.0 ) {
		FeatureDistribution overall;
		for(int i=0;i<syms.size();i++) 
			for(int j=0;j<SUB_PHASE_COUNT;j++) 
				for(int k=0;k<SUB_STATE_COUNT;k++)
					overall.CombineWithDistribution(syms[i].phase[j].state[k]);

		overall.ScaleWeightBy(blurSymbols/(syms.size()*SUB_PHASE_COUNT*SUB_STATE_COUNT));

		for(int i=0;i<syms.size();i++) 
			for(int j=0;j<SUB_PHASE_COUNT;j++) 
				for(int k=0;k<SUB_STATE_COUNT;k++) {
					if(syms[i].originalChar == 32 ||syms[i].originalChar == 0 ||syms[i].originalChar == 10) { //space,begin, or end
						learningTarget[i].phase[j].state[k].meanX[FEATURE_DENSITY] *= 1.0-blurSymbols;
						learningTarget[i].phase[j].state[k].meanX[FEATURE_DENSITY_MID] *= 1.0-blurSymbols;
						learningTarget[i].phase[j].state[k].meanX[FEATURE_DENSITY_HIGH_NEAR] *= 1.0-blurSymbols;
						learningTarget[i].phase[j].state[k].meanX[FEATURE_DENSITY_HIGH_NEAR_FIX] *= 1.0-blurSymbols;
						learningTarget[i].phase[j].state[k].meanX[FEATURE_DENSITY_LOW_NEAR] *= 1.0-blurSymbols;
						learningTarget[i].phase[j].state[k].meanX[FEATURE_DENSITY_MID_FIX] *= 1.0-blurSymbols;
					}
					learningTarget[i].phase[j].state[k].CombineWithDistribution(overall);
					learningTarget[i].phase[j].state[k].ScaleWeightBy(1.0/(1.0+blurSymbols));
				}

	}//endif blursymbols>0

#if LOGLEVEL >=9
	cout<<"LengthLearning: "<<overallTimer.elapsed()<<endl;
#endif
	learningTarget.iteration++;
}

