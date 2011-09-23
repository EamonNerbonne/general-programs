#include "StdAfx.h"
#include "WordSplitSolver.h"
#include <boost/timer.hpp>

using namespace std;
#ifndef NDEBUG
#ifndef _DEBUG
#define _DEBUG
#endif
#endif

#if  DO_CHECK_CONSISTENCY
inline int FindFirstNan(vector<double> const & vec) {
	for(int i=0;i<(int)vec.size();i++) {
		if(isnan(vec[i]))
			return i+1;
	}
	return 0;
}
#endif


#if  DO_CHECK_CONSISTENCY

inline void ErrIfNanImpl(vector<double> const & vec, char const * label) {
	int nanIdx = FindFirstNan(vec);
	if(nanIdx >0)
		cout<<label <<" contains NaN at index " << nanIdx-1 << endl;
}
#define ErrIfNan(x)  ErrIfNanImpl(x, #x)
#else
#define ErrIfNan(x) 
#endif

WordSplitSolver::WordSplitSolver(AllSymbolClasses const & syms, ImageFeatures const & imageFeatures, std::vector<short> const & targetString,std::vector<int> const & overrideEnds, double featureRelevance) 
	: syms(syms)
	, imageFeatures(imageFeatures)
	, imageLen1(imageFeatures.getImageWidth()/SOLVESCALE+1)
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
				featureLikelihoodSum += sym(u).phase[i].LogProbDensityOf(featsAt(xp));


#if !LENGTH_WEIGHT_ON_TERMINATORS
		if(u>0)
#endif
			lenLikelihoodSum += sym(u).LogLikelihoodLength(len*SOLVESCALE);
		splits.push_back(x*SOLVESCALE);
	}

	int U = (int)strLen()-1;
	int startX = x;
	int len =  imageLen() - startX;
	for(int i=0;i<SUB_PHASE_COUNT;i++) 
		for(int xp = startX + i*len/SUB_PHASE_COUNT;   xp < startX + (i+1)*len/SUB_PHASE_COUNT;   xp++)
			featureLikelihoodSum += sym(U).phase[i].LogProbDensityOf(featsAt(xp));

	splits.push_back(imageFeatures.getImageWidth());
#if LENGTH_WEIGHT_ON_TERMINATORS
	lenLikelihoodSum += sym(U).LogLikelihoodLength(len*SOLVESCALE);
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
	CheckSymConsistencyMsg(learningTarget, "before learning");

#if  DO_CHECK_CONSISTENCY
	if(FindFirstNan( this->op_x_u_i) >0||
		FindFirstNan( this->opC_x_u_i) >0||
		FindFirstNan( this->p_x_u) >0||
		FindFirstNan( this->P_x_u) >0||
		FindFirstNan( this->pb_x_u ) >0||
		FindFirstNan( this->pC_x_u ) >0||
		FindFirstNan( this->pCi_x_u_i ) >0||
		FindFirstNan( this->pf_x_u ) >0||
		FindFirstNan( this->Pi_x_u_i ) >0)
		cout<<"Nan!\n";
#endif


	for(int x=0;x<(int)imageLen();x++) {
		FeatureVector const & fv = featsAt(x);
		CheckSymConsistencyMsg(fv, "pix: " << x);

		for(int u=0;u<(int)strLen();u++) {
			int c=targetString[u];
			for(int i=0;i<SUB_PHASE_COUNT;i++) {
				if(!(Pi(x,u,i)>=0) ) 
					throw "NanOrNeg:Pi(x,u,i)";
				CheckSymConsistencyMsg(learningTarget[c].phase[i], "c:"<<c<<", i: "<<i);
				syms[c].phase[i].CombineInto(fv, Pi(x,u,i), learningTarget[c].phase[i]);
				CheckSymConsistencyMsg(learningTarget[c].phase[i], "c:"<<c<<", i: "<<i << " (after learning)");
			}
		}
	}
	CheckSymConsistencyMsg(learningTarget, "after feature learning");

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
			double ll = p(0,0) * p(x1,0+1) * exp(scOrig.LogLikelihoodLength((x1-0)*SOLVESCALE));
			if(isnan(ll)) throw "Nan: sym0 length likelihood";
			sc.LearnLength(double(x1*SOLVESCALE), ll );
		}
	}
#endif

	for(int u=1;u <(int)strLen()-1; u++) {
		SymbolClass & sc =  learningTarget[targetString[u]];
		SymbolClass const & scOrig =  syms[targetString[u]];

		for(unsigned i=0;i<imageLen1;i++) {
			double lenFactor= exp( scOrig.LogLikelihoodLength(i*SOLVESCALE));
			double sum =0.0;
			for(unsigned x0=0;x0<imageLen1-i;x0++) 
				sum+= p(x0,u) * p(x0+i,u+1);
			if(isnan(sum*lenFactor))
				throw "Nan: sym length likelihood";
			sc.LearnLength(double(i*SOLVESCALE),sum*lenFactor);
		}
	}
#if LENGTH_WEIGHT_ON_TERMINATORS
	{
		int U = strLen()-1;
		SymbolClass & sc =  learningTarget[targetString[U]];
		SymbolClass const & scOrig =  syms[targetString[U]];
		for(unsigned x0=0;x0<imageLen1;x0++) {
			double ll = p(x0,U) * exp(scOrig.LogLikelihoodLength((imageLen()-x0)*SOLVESCALE) );
			if(isnan(ll)) throw "Nan: symZ length likelihood";
			sc.LearnLength(double(imageLen()-x0)*SOLVESCALE, ll);
		}
	}
#endif
	CheckSymConsistencyMsg(learningTarget, "after length learning");

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
				CheckSymConsistencyMsg(learningTarget, "after blurring symbols");

	}//endif blursymbols>0

#if LOGLEVEL >=9
	cout<<"LengthLearning: "<<overallTimer.elapsed()<<endl;
#endif
	learningTarget.iteration++;
}

void WordSplitSolver::init_pCi_x_u_i() {
	using namespace std;
	pCi_x_u_i.resize(imageLen1*SUB_PHASE_COUNT*strLen());
	//for each phase, we want to count the number of previous writes to a particular pixel for averaging purposes.
	vector<int> xWriteCounts[SUB_PHASE_COUNT];
	for(int i=1;i<SUB_PHASE_COUNT;i++) //we don't actually use phase 0; that phase is after all equivalent to plain pC(x,u)
		xWriteCounts[i].resize(imageLen1);

	for(short u=0;u<strLen();u++) {
		for(int i=1;i<SUB_PHASE_COUNT;i++) {
			for(unsigned x=0;x<imageLen1;x++) {
				pCi(x,u,i) = 0.0;
				xWriteCounts[i][x]=0;
			}
		}
		for(unsigned x=0;x<imageLen1;x++) {
			pCi(x,u,0) = pC(x,u);
		}

		unsigned x0=0;
		unsigned x1=0;
		while(true) {
			double probU0 = pC(x0,u);
			double probU1 = u+1<strLen() ? pC(x1,u+1) : (x1<imageLen()?0.0:1.0); //prob of symbol u+1 having started at or before x1.  if u+1 is passed string end, prob == 0 unless x is passed image end.
			double lowerProb = std::min(probU0, probU1);

			//write prob.
			for(int i=1;i<SUB_PHASE_COUNT;i++) {
				int x =  ((SUB_PHASE_COUNT-i)*x0 + i*x1) / SUB_PHASE_COUNT;
				pCi(x, u, i) = (pCi(x, u, i)*xWriteCounts[i][x]+ lowerProb)/(xWriteCounts[i][x]+1);
				xWriteCounts[i][x]++;
			}

			//next, we raise the x with the lower prob (prefer raising x1?) but never beyond imageLen1-1
			if(x0==imageLen()) { //can't raise x0
				if(x1 == imageLen()) //ok, we're done!
					break;
				else
					x1++;
			} else if(x1 == imageLen()) { //can't raise x1, CAN raise x0.
				x0++;
			} else { //can raise both!
				if(probU0 < probU1)
					x0++;
				else
					x1++;
			}
		}
	}
	ErrIfNan(pCi_x_u_i);
}

void WordSplitSolver::init_pC_x_u() {
	using namespace std;
	pC_x_u.resize(imageLen1*strLen());
	for(short u=0;u<strLen();u++) {
		double sum = 0.0;
		for(unsigned x=0;x<imageLen1;x++) {
			sum += p(x,u);
			pC(x,u) = sum;
		}

		//we do a second pass to reduce errors. //doesn't seem to matter - errors still occur, and are not fatal.

		//double lastVal = 0.0;
		//double sumError=0.0;
		//for(unsigned x=0;x<imageLen1;x++) {
		//	double diff = p(x,u) - (pC(x,u) - lastVal); //OK, (pC(x,u) - sum) should be p(x,u), but let's say it's too big, then diff is negative by the amout pC(x,u) should be corrected by.
		//	sumError+=diff; //we accumulate all the errors in sumError; after all, the sum needs to be corrected for offset errors for all previous values of X.
		//	lastVal = pC(x,u);
		//	pC(x,u) += sumError;
		//}

		//sum = pC(imageLen1-1,u);

		for(unsigned x=0; x<imageLen1; x++) 
			pC(x,u) /= sum; //scaled to 0..1
	}
	ErrIfNan(pC_x_u);
}


void WordSplitSolver::init_P_x_u() {
	P_x_u.resize(imageLen()*strLen());

	short U = strLen()-1;

	bool err=false;
	for(unsigned x=0; x <imageLen(); x++) {
		for(short u=0; u<U; u++) {
			P(x,u) = pC(x,u) - pC(x,u+1); //should be positive.  Is it?
			if(isnan( P(x,u)))
				throw "P(x,u): NaN encountered; this should be impossible.";
			if(P(x,u)<0.0) {
				err=true;
				P(x,u) = 0.0;
			}
		}
		P(x,U) = pC(x,U); //probability of pC(x, U+1) -- i.e. of the symbol after the last in this string having started already -- is zero.
	}
	if(err)
		std::cout<<"!!";
	err=false;
	ErrIfNan(P_x_u);

	Pi_x_u_i.resize(imageLen()*strLen()*SUB_PHASE_COUNT);


	for(short u=0; u<strLen(); u++) {
		for(short i=0;i<SUB_PHASE_COUNT;i++) {
			short nextU = u+(i+1)/SUB_PHASE_COUNT;
			short nextI = (i+1)%SUB_PHASE_COUNT;
			for(unsigned x=0; x <imageLen(); x++) {
				if(nextU<strLen())
					Pi(x,u,i) = pCi(x,u,i) - pCi(x,nextU,nextI);
				else 
					Pi(x,u,i) = pCi(x,u,i); //probability of pCi(x, strLen(), ?) -- i.e. of the symbol after the last in this string having started already -- is zero.

				if(isnan( Pi(x,u,i) ))
					throw "Pi(x,u,i): NaN encountered; this should be impossible.";
				if(Pi(x,u,i)<0.0) { //should be positive.  Is it?
					err=true;
					std::cout<<"x"<<x<<" u"<<u<<" i"<<i<<"\n";
					Pi(x,u,i) = 0.0;
				}
			}
		}
	}
	if(err)
		std::cout<<"!";
	ErrIfNan(Pi_x_u_i);

}


void WordSplitSolver::init_p_x_u() {
	using namespace std;
	p_x_u.resize(imageLen1*strLen());
	//for(short u=0;u<strLen();u++) 
	//	for(unsigned x=0;x<imageLen1;x++) 
	//		p(x,u) = 0.0;

	for(unsigned x=0;x<imageLen1;x++) 
		p(x,0) = 0.0;
	p(0,0) = 1.0;//symbol 0 must start at 0.

	for(short u=1;u<strLen();u++) { //i.e. pb(?,0) and pf(?,U) are never used
		double sumL = 0.0;
		for(unsigned x=0;x<imageLen1;x++) {
			p(x,u) = pf(x,u-1) * pb(x,u); //likelihood of sym u starting precisely at x is pd of path upto u-1 ending at x times path from u starting at x.
			sumL += p(x,u);
		}
		for(unsigned x=0;x<imageLen1;x++) 
			p(x,u) /= sumL; //density is scaled relative so that sum is (about) 1.0
	}
	ErrIfNan(p_x_u);
}

void WordSplitSolver::init_pb_x_u() {
	using namespace std;
	pb_x_u.resize(imageLen1*strLen());

	for(short u=0;u<strLen();u++) 
		for(unsigned x=0;x<imageLen1;x++) 
			pb(x,u) = 0.0;


	short U = strLen()-1;
	unsigned X = imageLen1-1;

	SymbolClass const & scU = sym(U);
	for(unsigned x=0; x<imageLen1; x++) 
		pb(x,U) =  pb(x,U) + opR(U,x,X)
#if LENGTH_WEIGHT_ON_TERMINATORS
		*exp(scU.LogLikelihoodLength((X-x)*SOLVESCALE))
#endif
		;//first+last symbol have no intrinsic length 

	for(short u=U-1;u>=0; --u) {
		if(u==0 || overrideEnds[u-1]<0) {
			SymbolClass const & sc = sym(u);
			for(unsigned len=0;len<imageLen1;len++){
				double lenL = exp(sc.LogLikelihoodLength(len*SOLVESCALE));
				if(len<MIN_SYM_LENGTH) lenL*=exp(-0.5*(double(MIN_SYM_LENGTH)-len));
				for(unsigned x0=0;x0<imageLen1-len;x0++)  {
					unsigned x1 = x0 + len;
					pb(x0,u) = pb(x0,u) +  pb(x1, u + 1) *opR(u,x0,x1) *lenL;
				}
			}
		} else {
			int startX = overrideEnds[u-1]/SOLVESCALE;
			pb(startX,u) = 1.0;
			//for(unsigned x=startX; x<imageLen1; x++) {
			//	pb(startX,u) = pb(startX,u) +  pb(x, u+1);// * opR(u,startX,x);is this needed?
			//}
		}
	}
	ErrIfNan(pb_x_u);
}

void WordSplitSolver::init_pf_x_u() {
	using namespace std;
	pf_x_u.resize(imageLen1*strLen());

	for(short u=0;u<strLen();u++) 
		for(unsigned x=0;x<imageLen1;x++) 
			pf(x,u) = 0.0;


	SymbolClass const & sc0 = sym(0);
	for(unsigned x=0; x<imageLen1; x++) {
		pf(x,0) = pf(x,0) + opR(0,0,x)*exp(-0.1*x)
#if LENGTH_WEIGHT_ON_TERMINATORS
			*exp(sc0.LogLikelihoodLength((x-0)*SOLVESCALE))
#endif
			;//first+last symbol have no intrinsic length
		//	if(x<100&& x%5==0) cout<<pf(x,0)<<", ";
	}
	//cout<<"\n";

	for(short u=1;u<strLen();u++) {
		if(overrideEnds[u]<0) {
			SymbolClass const & sc = sym(u);
			for(unsigned len=0;len<imageLen1;len++){
				double lenL = exp(sc.LogLikelihoodLength(len*SOLVESCALE)); //avoiding the exp in the inner loop saves a bunch of time.
				if(len<MIN_SYM_LENGTH) lenL*=exp(-0.5*(double(MIN_SYM_LENGTH)-len));
				for(unsigned x0=0;x0<imageLen1-len;x0++)  {
					unsigned x1= x0 + len;
					pf(x1,u) = pf(x1,u) +  pf(x0, u-1) * opR(u,x0,x1) *lenL;
				}
			}
		} else {
			int endX = overrideEnds[u]/SOLVESCALE;
			pf(endX,u)=1.0;
			//for(unsigned x=0; (int)x<endX; x++) {
			//	pf(endX,u) = pf(endX,u) +  pf(x, u-1);// * opR(u,x,endX);is this needed?
			//}
		}
	}
	ErrIfNan(pf_x_u);
}


#if _MANAGED
#undef max
#endif

//double minimum

void WordSplitSolver::init_opC_x_u_i(double featureRelevanceFactor) {
	using namespace std;
	opC_x_u_i.resize(strLen()*SUB_PHASE_COUNT*imageLen1);

	double lowestLL=0.0; //maintains the lowest LL seen sofar per char, to avoid excessive LL rises.

	for(short u=0;u<strLen();u++ )
		for(short i=0;i<SUB_PHASE_COUNT;i++ )
			opC(0,u,i) =  0.0; //marginalize starting with 0

	for(unsigned x=1;x<imageLen1;x++) {
		double maxCL = -std::numeric_limits<double>::max();
		for(short u=0;u<strLen();u++ )
			for(short i=0;i<SUB_PHASE_COUNT;i++ ) {
				opC(x,u,i) = op(x-1,u,i)  + opC(x-1,u,i); //marginalize
				maxCL = max(maxCL,opC(x,u,i));
			}
			//so now we've found the cumulatively most likely
			for(short u=0;u<strLen();u++ )
				for(short i=0;i<SUB_PHASE_COUNT;i++ ){
					opC(x,u,i) -= maxCL;
					lowestLL = min(opC(x,u,i),lowestLL);
				}
				//so now we've found the cumulatively most likely relative to the maximum at this value of x.
				//if I wanted to work in non-log space, you'd need to ensure that for NO x0, x1, u with x0 < x1 that opC(x1, u) - opC(x1, u) > 1000 because of limitations in the exponent of a double - a diff. of 1000 is about 2^1000, which is still OK, barely.
	}
	//de-log-ize:
	double scaleFactor = lowestLL < -705 ? -705/lowestLL:1.0; //2^(-1023) ~ E^(-709), plus some safety margin.
	for(short u=0;u<strLen();u++ ) 
		for(short i=0;i<SUB_PHASE_COUNT;i++ )
			for(unsigned x=0;x<imageLen1;x++)
				opC(x,u,i) = exp(opC(x,u,i)*scaleFactor);
	ErrIfNan(opC_x_u_i);

}

void WordSplitSolver::init_op_x_u_i(double featureRelevanceFactor) {
	op_x_u_i.resize(imageLen()*strLen()*SUB_PHASE_COUNT);
	for(unsigned x=0;x<imageLen();++x) { //for all x-positions
		FeatureVector const & fv = featsAt(x);
		for(short ci=0;ci<(short)usedSymbols.size();ci++){ //for all used symbols
			short c = usedSymbols[ci];
			SymbolClass const & sc = this->syms[c];
			for(short i=0;i<SUB_PHASE_COUNT;i++) {//for each symbol-phase
				double logProbDensity = sc.phase[i].LogProbDensityOf(fv)*featureRelevanceFactor;//TODO: potential scaling factor initially to reduce impact?
				for(unsigned ui=0;ui<symToStrIdx[c].size();ui++) { //for each string position of a used symbol...
					short u = symToStrIdx[c][ui];
					op(x,u,i) = logProbDensity;
				}
			}
		}
	}
	ErrIfNan(op_x_u_i);
}

void WordSplitSolver::init_usedSymbols() { 
	for(short c=0;c<syms.size();c++) 
		if(!symToStrIdx[c].empty())
			usedSymbols.push_back(c);
}

void WordSplitSolver::init_symToStrIdx() { 
	for(short u=0;u<strLen();u++) 
		symToStrIdx[targetString[u]].push_back(u);
}
