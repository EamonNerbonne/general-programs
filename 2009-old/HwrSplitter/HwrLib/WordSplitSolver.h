#pragma once
#include "stdafx.h"
#include <boost/scoped_array.hpp>
#include "SymbolClass.h"
#include "feature/features.h"
#include <limits.h>
#include "LogNumber.h"

struct AllSymbolClasses {
	int symbolCount;
	boost::scoped_array<SymbolClass> sym;
	FeatureVector featureWeights;
	AllSymbolClasses(int symbolCount) : symbolCount(symbolCount), sym(new SymbolClass[symbolCount]) {	
		for(int i=0;i<NUMBER_OF_FEATURES;i++)
			featureWeights[i]=1.0;
	}
	SymbolClass & operator[](short symbol) {return sym[symbol];}
	SymbolClass const & getSymbol(short symbol) const {return sym[symbol];}
	SymbolClass & getSymbol(short symbol) {return sym[symbol];}
	short size() {return symbolCount;}
	void initRandom() {
		for(int i=0;i<symbolCount;i++)
			sym[i].initRandom();
	}
	int AllocatedSize() {return sizeof(AllSymbolClasses) + sizeof(SymbolClass)*symbolCount;}
	void RecomputeFeatureWeights(){
		FeatureDistribution overall;
		FeatureDistribution means;
		for(int i=0;i<size();i++) {
			for(int j=0;j<SUB_SYMBOL_COUNT;j++) {
				overall.CombineWith(sym[i].state[j]);
				means.CombineWith(sym[i].state[j].meanX, sym[i].state[j].weightSum);
			}
		}
		//OK, we have the overall variance and the variance of the means.  Where the means variance is high in relation to the overall variance, most variation is inter rather than intra-class.
		double maxWeight=0.0;
		for(int i=0;i<NUMBER_OF_FEATURES;i++) {
			featureWeights[i] = means.varX(i) / overall.varX(i);
			maxWeight = std::max(maxWeight,featureWeights[i]);
		}
		for(int i=0;i<NUMBER_OF_FEATURES;i++)
			featureWeights[i]/=maxWeight;
	}
};

class WordSplitSolver
{
	//========================
	//image + string details:  initialized in contructor:
	AllSymbolClasses & syms; //details of the prob. distribution for the features of the various symbols.
	ImageFeatures const & imageFeatures;//details of the features of the image at various x-coordinates.
	const unsigned imageLen1;//number of possible starting points for a symbol: imageLen()+1 since a symbol may have zero length and thus start after the last pixel ends.
	std::vector<short> const & targetString;
	inline unsigned imageLen() { return imageFeatures.getImageWidth(); }
	inline short strLen() { return (short) targetString.size(); }
	inline SymbolClass & sym(short u) {return syms[targetString[u]];}

	//lookuptable from char value to vector of those string indexes containing that value.
	boost::scoped_array< vector<short> > symToStrIdx; //initialized in constuctor.
	void init_symToStrIdx() { 
		for(short u=0;u<strLen();u++) 
			symToStrIdx[targetString[u]].push_back(u);
	}

	//========================
	//list of all used character values (i.e. those present at least once in the string), in ascending character value order.
	vector<short> usedSymbols;
	void init_usedSymbols() { 
		for(short c=0;c<syms.size();c++) 
			if(!symToStrIdx[c].empty())
				usedSymbols.push_back(c);
	}
	//========================
	//===op(x,u,i)===
	//The log-likelihood symbol u_i (sym:u subsym:i) was observed at pixel x
	//This value is relative to those of other symbols x, and may be scaled.  
	//The scaling factor is identical for all symbols; i.e. may vary per x but not per symbol
	//op(x,u,i) == log(pd_u_i(x) * k(x)) for some scaling factor k(x).
	std::vector<double> op_x_u_i;
	inline double & op(unsigned x, short u,short i) { return op_x_u_i[x*strLen()*SUB_SYMBOL_COUNT +  u*SUB_SYMBOL_COUNT + i];	}
	void init_op_x_u_i(double featureRelevanceFactor) {
		op_x_u_i.resize(imageLen()*strLen()*SUB_SYMBOL_COUNT);
		for(unsigned x=0;x<imageLen();++x) { //for all x-positions
			FeatureVector const & fv = imageFeatures.featAt(x);
			for(short ci=0;ci<(short)usedSymbols.size();ci++){ //for all used symbols
				short c = usedSymbols[ci];
				SymbolClass & sc = this->syms[c];
				for(short i=0;i<SUB_SYMBOL_COUNT;i++) {//for each sub-symbol
					double logProbDensity = sc.state[i].LogProbDensityOf(fv,syms.featureWeights)*featureRelevanceFactor;//TODO: potential scaling factor initially to reduce impact?
					for(unsigned ui=0;ui<symToStrIdx[c].size();ui++) { //for each string position of a used symbol...
						short u = symToStrIdx[c][ui];
						op(x,u,i) = logProbDensity;
					}
				}
			}
		}
	}

	//========================
	//===opC(x,u)===
	//the cumulative log-likelihood that u_i for any i was observed on pixels [0..x)
	std::vector<double> opC_x_u;
	inline double & opC(unsigned x, short u) { return opC_x_u[u*imageLen1 + x];	}
	inline double opR(short u, unsigned x0, unsigned x1) {return opC(x1,u) / opC(x0,u);}//the cumulative log-likelihood that u_i was observed on px [x0..x1)
	void init_opC_x_u() {
		using namespace std;
		opC_x_u.resize(imageLen1*strLen());

		double lowestLL=0.0; //maintains the lowest LL seen sofar per char, to avoid excessive LL rises.


		for(short u=0;u<strLen();u++ ) {
			opC(0,u) =  0.0; //marginalize
		}
		for(unsigned x=1;x<imageLen1;x++) {
			double maxCL(-numeric_limits<double>::max());
			for(short u=0;u<strLen();u++ ) {
				double maxL(-numeric_limits<double>::max());
				for(short i=0;i<SUB_SYMBOL_COUNT;i++) 
					maxL = max(maxL, op(x-1,u,i));

				//Effectively: "op(x-1,u) == maxL"

				opC(x,u) = maxL + opC(x-1,u); //marginalize
				maxCL = max(maxCL, opC(x,u));
			}
			//so now we've found the cumulatively most likely
			for(short u=0;u<strLen();u++ ) {
				opC(x,u) -= maxCL;
				lowestLL = min(opC(x,u),lowestLL);
			}
			//so now we've found the cumulatively most likely relative to the maximum at this value of x.
			//if I wanted to work in non-log space, you'd need to ensure that for NO x0, x1, u with x0 < x1 that opC(x1, u) - opC(x1, u) > 1000 because of limitations in the exponent of a double - a diff. of 1000 is about 2^1000, which is still OK, barely.
		}
		
		
		//delogize:
		double scaleFactor = lowestLL < -320 ? -320/lowestLL:1.0; //2^(-1023) ~ 10^(-324), plus some safety margin.
		for(short u=0;u<strLen();u++ ) 
			for(unsigned x=0;x<imageLen1;x++)
				opC(x,u) = exp(opC(x,u)*scaleFactor);
	}

	//========================
	//the (nonlog)likelihood of observing symbols [0,u] precisely over pixels [0,x), i.e. of u ending at x.
	std::vector<double> pf_x_u;
	inline double & pf(unsigned x, short u) { return pf_x_u[u*imageLen1 + x]; }
	void init_pf_x_u() {
		using namespace std;
		pf_x_u.resize(imageLen1*strLen());

		for(short u=0;u<strLen();u++) 
			for(unsigned x=0;x<imageLen1;x++) 
				pf(x,u) = 0.0;


		SymbolClass const & sc0 = sym(0);
		for(unsigned x=0; x<imageLen1; x++) {
			pf(x,0) = pf(x,0) + opR(0,0,x)*exp(sc0.LogLikelihoodLength(x-0));
		}


		for(short u=1;u<strLen();u++) {
			SymbolClass const & sc = sym(u);
			for(unsigned len=0;len<imageLen1;len++){
				double lenL = exp(sc.LogLikelihoodLength(len)); //avoiding the exp in the inner loop saves a bunch of time.
				if(len<5) lenL*=exp(-0.5*(5.0-len));
				for(unsigned x0=0;x0<imageLen1-len;x0++)  {
					unsigned x1= x0 +len;
					pf(x1,u) = pf(x1,u) +  pf(x0, u-1) * opR(u,x0,x1) *lenL;
				}

			}

		}
	}

	//========================
	//the (nonlog)likelihood of observing symbols [u,U] precisely over pixels [x,X), i.e. of u beginning at x.
	std::vector<double> pb_x_u;
	inline double & pb(unsigned x, short u) { return pb_x_u[u*imageLen1 + x]; }
	void init_pb_x_u() {
		using namespace std;
		pb_x_u.resize(imageLen1*strLen());

		for(short u=0;u<strLen();u++) 
			for(unsigned x=0;x<imageLen1;x++) 
				pb(x,u) = 0.0;


		short U = strLen()-1;
		unsigned X = imageLen1-1;

		SymbolClass const & scU = sym(U);
		for(unsigned x=0; x<imageLen1; x++) 
			pb(x,U) =  pb(x,U) + opR(U,x,X) *exp(scU.LogLikelihoodLength(X-x));   

		for(short u=U-1;u>=0; --u) {
			SymbolClass const & sc = sym(u);
			for(unsigned len=0;len<imageLen1;len++){
				double lenL = exp(sc.LogLikelihoodLength(len));
				if(len<5) lenL*=exp(-0.5*(5.0-len));
				for(unsigned x0=0;x0<imageLen1-len;x0++)  {
					unsigned x1 = x0 + len;
					pb(x0,u) = pb(x0,u) +  pb(x1, u + 1) *opR(u,x0,x1) *lenL;
				}
			}
		}
	}

	//========================
	//the (nonlog)likelihood of symbol u starting precisely at x.
	//May be off by a factor k(u), i.e. for any k(u) \forall x:  p(x,u) ~ k(u)* p'(x,u)
	std::vector<double> p_x_u;
	inline double & p(unsigned x, short u) { return p_x_u[u*imageLen1+ x]; }
	void init_p_x_u() {
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
	}

	//========================
	//the likelihood (not log!) of symbol u starting before (or just at) x
	std::vector<double> pC_x_u;
	inline double & pC(unsigned x, short u) { return pC_x_u[u*imageLen1+ x]; }
	void init_pC_x_u() {
		using namespace std;
		pC_x_u.resize(imageLen1*strLen());
		for(short u=0;u<strLen();u++) {
			double sum = 0.0;
			for(unsigned x=0;x<imageLen1;x++) {
				sum += p(x,u);
				pC(x,u) = sum;
			}

			//we do a second pass to reduce errors.

			double lastVal = 0.0;
			double sumError=0.0;
			for(unsigned x=0;x<imageLen1;x++) {
				double diff = p(x,u) - (pC(x,u) - lastVal); //OK, (pC(x,u) - sum) should be p(x,u), but let's say it's too big, then diff is negative by the amout pC(x,u) should be corrected by.
				sumError+=diff; //we accumulate all the errors in sumError; after all, the sum needs to be corrected for offset errors for all previous values of X.
				lastVal = pC(x,u);
				pC(x,u) += sumError;
			}

			sum = pC(imageLen1-1,u);

			for(unsigned x=0;x<imageLen1;x++) 
				pC(x,u) /= sum; //scaled to 0..1
		}
	}

	//========================
	//the probability that pixel x is in symbol u. -- u in [ 0..strLen() ), x in [ 0..imageLen() )
	std::vector<double> P_x_u;
	inline double & P(unsigned x, short u) { return P_x_u[x*strLen() +  u]; }
	void init_P_x_u() {
		P_x_u.resize(imageLen()*strLen());

		short U = strLen()-1;

		for(unsigned x=0; x <imageLen(); x++) {
			for(short u=0; u<U; u++) {
				P(x,u) = pC(x,u) - pC(x,u+1); //should be positive.  Is it?
				if(P(x,u)<0.0) {
					std::cout<<"!";
					P(x,u) = 0.0;
				}
			}
			P(x,U) = pC(x,U); //probability of pC(x, U+1) -- i.e. of the symbol after the last in this string having started already -- is zero.
		}
	}


public:
	WordSplitSolver(AllSymbolClasses & syms, ImageFeatures const & imageFeatures, std::vector<short> const & targetString,double featureRelevance) ;
	void Learn(double blurSymbols);
	vector<int> MostLikelySplit(double & loglikelihood);
};
