#ifndef HWRCONFIG_H
#define HWRCONFIG_H

#pragma warning (disable:4996)

inline static double sqr(double x) { return x*x; }

#define LOGLEVEL 1
#define LENGTH_WEIGHT_ON_TERMINATORS 0

#define SUB_STATE_COUNT 4 //more than one looks counter-productive - at least initially, hard to say about later
#define SUB_PHASE_COUNT 3
#define DYNAMIC_SYMBOL_WEIGHT 0.0
#define FEATURE_SCALING 1.0
//this is in relation to length.


double const DefaultFeatureWeight = 1.0;
double const DefaultFeatureVariance = sqr(1000);

#define DO_CHECK_CONSISTENCY 0

#define FEATURE_BLUR 1
#define MIN_SYM_LENGTH 7
inline bool isnan(double x) { return x != x; }


#if DO_CHECK_CONSISTENCY
#define CheckSymConsistency(x) { if((x).CheckConsistency()>0) std::cout<<#x<<" is inconsistent!\n"; }
#define CheckSymConsistencyMsg(x,msg) { if((x).CheckConsistency()>0) std::cout<<#x<<" is inconsistent: "<<msg <<std::endl; }

#else 
#define CheckSymConsistencyMsg(x, msg)
#define CheckSymConsistency(x)
#endif


#endif

