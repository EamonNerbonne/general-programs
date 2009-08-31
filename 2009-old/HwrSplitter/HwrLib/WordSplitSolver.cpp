#include "StdAfx.h"
#include "WordSplitSolver.h"
#include <boost/timer.hpp>

WordSplitSolver::WordSplitSolver(AllSymbolClasses & syms, ImageFeatures const & imageFeatures, std::vector<short> const & targetString, double featureRelevance) 
: syms(syms)
, imageFeatures(imageFeatures)
, imageLen1(imageFeatures.getImageWidth()+1)
, targetString(targetString) 
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
	init_opC_x_u();

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
			double maxL=-numeric_limits<double>::max();
			for(int j=0;j<SUB_SYMBOL_COUNT;j++)
				maxL= max(sym(u).state[j].LogProbDensityOf(imageFeatures.featAt(x),syms.featureWeights) ,maxL);
			featureLikelihoodSum+=maxL;
			x++;
		}
		lenLikelihoodSum += sym(u).LogLikelihoodLength(x-startX);
		splits.push_back(x);
	}
	
	int U = (int)strLen()-1;
	int startX = x;

	while(x <(int) imageLen() ) { 
		double maxL=-numeric_limits<double>::max();
		for(int j=0;j<SUB_SYMBOL_COUNT;j++)
			maxL= max(sym(U).state[j].LogProbDensityOf(imageFeatures.featAt(x),syms.featureWeights) ,maxL);
		featureLikelihoodSum+=maxL;
		x++;
	}
	lenLikelihoodSum += sym(U).LogLikelihoodLength(imageLen()-startX);

	splits.push_back(imageLen());


	loglikelihood = featureLikelihoodSum/imageLen() + lenLikelihoodSum/strLen();

	return splits;
}


void WordSplitSolver::Learn(double blurSymbols){
	using namespace std;
	using namespace boost;

	timer overallTimer;

	//we need to "guess" the sub-state loglikelihoods: we've only the overall probability P(x,u)

	for(int x=0;x<(int)imageLen();x++) {
		FeatureVector const & fv = imageFeatures.featAt(x);
		for(int u=0;u<(int)strLen();u++) {
			double maxLL(-numeric_limits<double>::max());
			int maxI=0;
			for(int i=0;i<SUB_SYMBOL_COUNT;i++) {
				if(maxLL < op(x,u,i)) {
					maxLL = op(x,u,i);
					maxI = i;
				}
			}

			//we know the _total_ probability that u is observerd at x, and now we presume that this total probability
			//can be proportionally divided across its state according to their likelihood

			int c=targetString[u];
			for(int i=0;i<SUB_SYMBOL_COUNT;i++) 
				syms[c].state[i].CombineWith(fv, P(x,u) * exp( op(x,u,i) - maxLL + (i==maxI?0:-1)  )  );
		}
	}
	for(int u=0;u<(int)strLen();u++) {
		sym(u).RecomputeDCoffset();
#if LOGLEVEL >= 10 
		cout<<"sym["<<targetString[u]<<"]: w="<<sym(u).state[0].getWeightSum() <<"; DC="<<sym(u).state[0].getDCoffset()<<"\n"; 
#endif
	}



#if LOGLEVEL >= 9 
	cout<<"SymbolLearning: "<<overallTimer.elapsed()<<endl;
#endif
	overallTimer.restart();

	//now we'd like to somehow compute the expected length+ variance of each symbol.

	vector<double> lengthWeight(imageLen1);
	{
		SymbolClass & sc = sym(0);
		for(unsigned x1=0;x1<imageLen1;x1++) {
			sc.LearnLength(Float(x1), p(0,0) * p(x1,0+1) * exp(sc.LogLikelihoodLength(x1-0)) );
		}
    }

	for(int u=1;u <(int)strLen()-1; u++) {
		SymbolClass & sc = sym(u);

		for(unsigned i=0;i<imageLen1;i++) {
			double lenFactor= exp( sc.LogLikelihoodLength(i));
			double sum =0.0;
			for(unsigned x0=0;x0<imageLen1-i;x0++) 
				sum+= p(x0,u) * p(x0+i,u+1);
			sc.LearnLength(Float(i),sum*lenFactor);
		}
	}

	{
		int U = strLen()-1;
		SymbolClass & sc = sym(U);
		for(unsigned i=0;i<imageLen1;i++)
			lengthWeight[i]=0.0;

		for(unsigned x0=0;x0<imageLen1;x0++) 
			lengthWeight[imageLen()-x0] += p(x0,U) * exp(sc.LogLikelihoodLength(imageLen()-x0));

		for(unsigned i=0;i<imageLen1;i++)
			sc.LearnLength(Float(i),lengthWeight[i]);
	}

	if(blurSymbols != 0.0 ) {
		FeatureDistribution overall;
		for(int i=0;i<syms.size();i++) {
			for(int j=0;j<SUB_SYMBOL_COUNT;j++) {
				overall.CombineWith(syms[i].state[j]);
			}
		}

		overall.ScaleWeightBy(blurSymbols/syms.size()*SUB_SYMBOL_COUNT);
		for(int i=0;i<syms.size();i++) {
			for(int j=0;j<SUB_SYMBOL_COUNT;j++) {
				if(syms[i].originalChar == 32 ||syms[i].originalChar == 0 ||syms[i].originalChar == 10) { //space,begin, or end
					syms[i].state[j].meanX[FEATURE_DENSITY] *= 1.0-blurSymbols;
					syms[i].state[j].meanX[FEATURE_DENSITY_MID] *= 1.0-blurSymbols;
				}
				syms[i].state[j].CombineWith(overall);
				syms[i].state[j].ScaleWeightBy(1.0/(1.0+blurSymbols));
				syms[i].state[j].RecomputeDCfactor();
			}
		}
	}
	for(int i=0;i<syms.size();i++) {
		syms[i].ScaleWeightBy(0.9999307);//halve over 10000 iterations
	}
	syms.RecomputeFeatureWeights();


#if LOGLEVEL >=10
	for(int ui=0;ui<usedSymbols.size();ui++) {
		SymbolClass const & sc = syms[usedSymbols[ui]];
		cout << "sc("<<usedSymbols[ui]<<").len= "<<sc.meanLength() <<" +/- "<<sqrt(sc.varLength())<<";  ["<<sc.weightLength() <<"]";
		for(int ci =0; ci<symToStrIdx[usedSymbols[ui]].size(); ci++)
			cout<< symToStrIdx[usedSymbols[ui]][ci] << ";";
		cout <<"\n";
	}
#endif
#if LOGLEVEL >=9
	cout<<"LengthLearning: "<<overallTimer.elapsed()<<endl;
#endif


}

