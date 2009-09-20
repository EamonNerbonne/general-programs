#pragma once
#include "stdafx.h"
#include "AllSymbolClasses.h"
#include "feature/features.h"
#include <limits.h>

class WordSplitSolver
{
	//========================
	//image + string details:  initialized in contructor:
	AllSymbolClasses const & syms; //details of the prob. distribution for the features of the various symbols.
	ImageFeatures const & imageFeatures;//details of the features of the image at various x-coordinates.
	const unsigned imageLen1;//number of possible starting points for a symbol: imageLen()+1 since a symbol may have zero length and thus start after the last pixel ends.
	std::vector<short> const & targetString;
	std::vector<int> const & overrideEnds;
	inline unsigned imageLen() { return imageFeatures.getImageWidth(); }
	inline short strLen() { return (short) targetString.size(); }
	inline SymbolClass const & sym(short u) {return syms[targetString[u]];}

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
	inline double & op(unsigned x, short u,short i) { return op_x_u_i[x*strLen()*SUB_PHASE_COUNT +  u*SUB_PHASE_COUNT + i];	}
	void init_op_x_u_i(double featureRelevanceFactor) {
		op_x_u_i.resize(imageLen()*strLen()*SUB_PHASE_COUNT);
		for(unsigned x=0;x<imageLen();++x) { //for all x-positions
			FeatureVector const & fv = imageFeatures.featAt(x);
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
	}

	//========================
	//===opC(x,u)===
	//the cumulative log-likelihood that u_i for any i was observed on pixels [0..x) Il1|
	std::vector<double> opC_x_u_i;
	inline double & opC(unsigned x, short u, short i) { return opC_x_u_i[u*imageLen1*SUB_PHASE_COUNT + i*imageLen1 + x];	}
	inline double opsR(short u, short i,unsigned x0, unsigned x1) { return opC(x1,u,i) / opC(x0,u,i);}
	inline double opR(short u, unsigned x0, unsigned x1) {//the cumulative log-likelihood that u was observed on px [x0..x1)
		unsigned len = x1 - x0;
		double ll =1.0;
		for(int i=0;i<SUB_PHASE_COUNT;i++)
			ll*=opsR(u,i, x0 + i*len/SUB_PHASE_COUNT, x0 + (i+1)*len/SUB_PHASE_COUNT);
		return ll;
	}
	void init_opC_x_u_i(double featureRelevanceFactor) {
		using namespace std;
		opC_x_u_i.resize(strLen()*SUB_PHASE_COUNT*imageLen1);

		double lowestLL=0.0; //maintains the lowest LL seen sofar per char, to avoid excessive LL rises.

		for(short u=0;u<strLen();u++ )
			for(short i=0;i<SUB_PHASE_COUNT;i++ )
				opC(0,u,i) =  0.0; //marginalize starting with 0

		for(unsigned x=1;x<imageLen1;x++) {
			double maxCL(-numeric_limits<double>::max());
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
			pf(x,0) = pf(x,0) + opR(0,0,x)*exp(-0.1*x)
#if LENGTH_WEIGHT_ON_TERMINATORS
				*exp(sc0.LogLikelihoodLength(x-0))
#endif
				;//first+last symbol have no intrinsic length
		//	if(x<100&& x%5==0) cout<<pf(x,0)<<", ";
		}
        //cout<<"\n";

		for(short u=1;u<strLen();u++) {
			if(overrideEnds[u]<0) {
				SymbolClass const & sc = sym(u);
				for(unsigned len=0;len<imageLen1;len++){
					double lenL = exp(sc.LogLikelihoodLength(len)); //avoiding the exp in the inner loop saves a bunch of time.
					if(len<MIN_SYM_LENGTH) lenL*=exp(-0.5*(double(MIN_SYM_LENGTH)-len));
					for(unsigned x0=0;x0<imageLen1-len;x0++)  {
						unsigned x1= x0 +len;
						pf(x1,u) = pf(x1,u) +  pf(x0, u-1) * opR(u,x0,x1) *lenL;
					}
				}
			} else {
				int endX = overrideEnds[u];
				pf(endX,u)=1.0;
				//for(unsigned x=0; (int)x<endX; x++) {
				//	pf(endX,u) = pf(endX,u) +  pf(x, u-1);// * opR(u,x,endX);is this needed?
				//}
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
			pb(x,U) =  pb(x,U) + opR(U,x,X)
#if LENGTH_WEIGHT_ON_TERMINATORS
				*exp(scU.LogLikelihoodLength(X-x))
#endif
			;//first+last symbol have no intrinsic length 

		for(short u=U-1;u>=0; --u) {
			if(u==0 || overrideEnds[u-1]<0) {
				SymbolClass const & sc = sym(u);
				for(unsigned len=0;len<imageLen1;len++){
					double lenL = exp(sc.LogLikelihoodLength(len));
					if(len<MIN_SYM_LENGTH) lenL*=exp(-0.5*(double(MIN_SYM_LENGTH)-len));
					for(unsigned x0=0;x0<imageLen1-len;x0++)  {
						unsigned x1 = x0 + len;
						pb(x0,u) = pb(x0,u) +  pb(x1, u + 1) *opR(u,x0,x1) *lenL;
					}
				}
			} else {
				int startX = overrideEnds[u-1];
				pb(startX,u) = 1.0;
				//for(unsigned x=startX; x<imageLen1; x++) {
				//	pb(startX,u) = pb(startX,u) +  pb(x, u+1);// * opR(u,startX,x);is this needed?
				//}
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

			for(unsigned x=0;x<imageLen1;x++) 
				pC(x,u) /= sum; //scaled to 0..1
		}
	}

	//========================
	//the likelihood (not log!) of symbol u phase i starting before (or just at) x
	std::vector<double> pCi_x_u_i;
	inline double & pCi(unsigned x, short u,short i) { return pCi_x_u_i[u*imageLen1*SUB_PHASE_COUNT + i*imageLen1+ x]; }
	void init_pCi_x_u_i() {
		using namespace std;
		pCi_x_u_i.resize(imageLen1*SUB_PHASE_COUNT*strLen());
		for(short u=0;u<strLen();u++) {
			int xLastSet[SUB_PHASE_COUNT];
			double xUpto[SUB_PHASE_COUNT];
			for(int i=0;i<SUB_PHASE_COUNT;i++) {
				xLastSet[i]=0;
				xUpto[i]=0.0;
			}
			double lastProb = 0.0;
			unsigned x1=0;
			double x1p=0.0;
			for(unsigned x0=0; x0<imageLen1; x0++) {
				if(x1<x0) x1=x0;
				
				double prob = pC(x0, u);
				if(u+1<strLen()) {
					while( x1 < imageLen1 && prob >= pC(x1, u+1))
						x1++;
					x1p = x1==0? 0.0 : (double)x1 - (pC(x1, u+1) - prob) / (pC(x1, u+1) - pC(x1-1,u+1));
					if(x1p>imageLen1 - 1) x1p = imageLen1 - 1;
				} else
					x1p = imageLen1 - 1;

				for(int i=0;i<SUB_PHASE_COUNT;i++) {
					double newPos = (x1p - x0)*i/double(SUB_PHASE_COUNT) + x0;
					int xMaxSet =(int) (newPos+0.5);
					for(int x=xLastSet[i];x <xMaxSet;x++) {
						//linear interpolation so that pCi(xUpto[i],u,i) == lastProb and pCi(newPos,u,i) == prob
						pCi(x,u,i) = (x - xUpto[i]) / (newPos - xUpto[i]) * (prob - lastProb) + lastProb;
					}
					xUpto[i] = newPos;
					xLastSet[i] = xMaxSet;
				}
			}
			for(int i=0;i<SUB_PHASE_COUNT;i++) 
				for(int x=xLastSet[i];x<(int)imageLen1;x++)
					pCi(x,u,i) = 1.0;
		}
	}


	//========================
	//the probability that pixel x is in symbol u. -- u in [ 0..strLen() ), x in [ 0..imageLen() )
	std::vector<double> P_x_u;
	inline double & P(unsigned x, short u) { return P_x_u[u*imageLen() + x]; }
	std::vector<double> Pi_x_u_i;
	inline double & Pi(unsigned x, short u,short i) { return P_x_u[u*imageLen()*SUB_PHASE_COUNT + i*imageLen()+ x]; }
	void init_P_x_u() {
		P_x_u.resize(imageLen()*strLen());

		short U = strLen()-1;
	
		bool err=false;
		for(unsigned x=0; x <imageLen(); x++) {
			for(short u=0; u<U; u++) {
				P(x,u) = pC(x,u) - pC(x,u+1); //should be positive.  Is it?
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

		for(short u=0; u<strLen(); u++) {
			for(short i=0;i<SUB_PHASE_COUNT;i++) {
				short nextU = u+(i+1)/SUB_PHASE_COUNT;
				short nextI = (i+1)%SUB_PHASE_COUNT;
				for(unsigned x=0; x <imageLen(); x++) {
					if(nextU<strLen())
						Pi(x,u,i) = pCi(x,u,i) - pCi(x,nextU,nextI);
					else 
						Pi(x,u,i) = pCi(x,u,i); //probability of pCi(x, strLen(), ?) -- i.e. of the symbol after the last in this string having started already -- is zero.
					if(Pi(x,u,i)<0.0) { //should be positive.  Is it?
						err=true;
						Pi(x,u,i) = 0.0;
					}
				}
			}
		}
		if(err)
			std::cout<<"!";
	}


public:
	WordSplitSolver(AllSymbolClasses const & syms, ImageFeatures const & imageFeatures, std::vector<short> const & targetString, std::vector<int> const & overrideEnds, double featureRelevance) ;
	void Learn(double blurSymbols, AllSymbolClasses& learningTarget);
	vector<int> MostLikelySplit(double & loglikelihood);
};
