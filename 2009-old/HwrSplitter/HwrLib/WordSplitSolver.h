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
	inline unsigned imageLen() { return imageFeatures.getImageWidth()/SOLVESCALE; }
	FeatureVector const& featsAt(int x) {return imageFeatures.featAt(SOLVESCALE*x);}

	inline short strLen() { return (short) targetString.size(); }
	inline SymbolClass const & sym(short u) {return syms[targetString[u]];}

	//lookuptable from char value to vector of those string indexes containing that value.
	boost::scoped_array< vector<short> > symToStrIdx; //initialized in constuctor.
	void init_symToStrIdx();

	//========================
	//list of all used character values (i.e. those present at least once in the string), in ascending character value order.
	vector<short> usedSymbols;
	void init_usedSymbols();


	//========================
	//===op(x,u,i)===
	//The log-likelihood symbol u_i (sym:u subsym:i) was observed at pixel x
	//This value is relative to those of other symbols x, and may be scaled.  
	//The scaling factor is identical for all symbols; i.e. may vary per x but not per symbol
	//op(x,u,i) == log(pd_u_i(x) * k(x)) for some scaling factor k(x).
	std::vector<double> op_x_u_i;
	inline double & op(unsigned x, short u,short i) { return op_x_u_i[x*strLen()*SUB_PHASE_COUNT +  u*SUB_PHASE_COUNT + i];	}
	void init_op_x_u_i(double featureRelevanceFactor);

	//========================
	//===opC(x,u)===
	//the cumulative log-likelihood that u_i for any i was observed on pixels [0..x) Il1|
	std::vector<double> opC_x_u_i;
	inline double & opC(unsigned x, short u, short i) { return opC_x_u_i[u*imageLen1*SUB_PHASE_COUNT + i*imageLen1 + x];	}
	inline double opsR(short u, short i,unsigned x0, unsigned x1) { return opC(x1,u,i) / opC(x0,u,i);}
	inline double opR(short u, unsigned x0, unsigned x1) {//the cumulative log-likelihood that u was observed on px [x0..x1)
		unsigned len = x1 - x0;
		double ll =1.0;
		for(short i=0;i<SUB_PHASE_COUNT;i++)
			ll*=opsR(u,i, x0 + i*len/SUB_PHASE_COUNT, x0 + (i+1)*len/SUB_PHASE_COUNT);
		return ll;
	}
	void init_opC_x_u_i();

	//========================
	//the (nonlog)likelihood of observing symbols [0,u] precisely over pixels [0,x), i.e. of u ending at x.
	std::vector<double> pf_x_u;
	inline double & pf(unsigned x, short u) { return pf_x_u[u*imageLen1 + x]; }
	void init_pf_x_u();

	//========================
	//the (nonlog)likelihood of observing symbols [u,U] precisely over pixels [x,X), i.e. of u beginning at x.
	std::vector<double> pb_x_u;
	inline double & pb(unsigned x, short u) { return pb_x_u[u*imageLen1 + x]; }
	void init_pb_x_u();

	//========================
	//the (nonlog)likelihood of symbol u starting precisely at x.
	//May be off by a factor k(u), i.e. for any k(u) \forall x:  p(x,u) ~ k(u)* p'(x,u)
	std::vector<double> p_x_u;
	inline double & p(unsigned x, short u) { return p_x_u[u*imageLen1+ x]; }
	void init_p_x_u();

	//========================
	//the likelihood (not log!) of symbol u starting before (or just at) x
	std::vector<double> pC_x_u;
	inline double & pC(unsigned x, short u) { return pC_x_u[u*imageLen1+ x]; }
	void init_pC_x_u();

	//========================
	//the likelihood (not log!) of symbol u phase i starting before (or just at) x
	std::vector<double> pCi_x_u_i;
	inline double & pCi(unsigned x, short u,short i) { return pCi_x_u_i[u*imageLen1*SUB_PHASE_COUNT + i*imageLen1+ x]; }
	void init_pCi_x_u_i();

	//========================
	//the probability that pixel x is in symbol u. -- u in [ 0..strLen() ), x in [ 0..imageLen() )
	std::vector<double> P_x_u;
	inline double & P(unsigned x, short u) { return P_x_u[u*imageLen() + x]; }
	std::vector<double> Pi_x_u_i;
	inline double & Pi(unsigned x, short u,short i) { return Pi_x_u_i[u*imageLen()*SUB_PHASE_COUNT + i*imageLen()+ x]; }
	void init_P_x_u();//also inits Pi_x_u_i


public:
	WordSplitSolver(AllSymbolClasses const & syms, ImageFeatures const & imageFeatures, std::vector<short> const & targetString, std::vector<int> const & overrideEnds, double featureRelevance) ;
	void Learn(double blurSymbols, AllSymbolClasses& learningTarget);
	vector<int> MostLikelySplit(double & loglikelihood);
};
