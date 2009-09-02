#ifndef HWRCONFIG_H
#define HWRCONFIG_H

#pragma warning (disable:4996)
typedef double Float;
inline static Float sqr(Float x) { return x*x; }

#define LOGLEVEL 1
#define LENGTH_WEIGHT_ON_TERMINATORS (1==0)

#define SUB_SYMBOL_COUNT 1 //more than one looks counter-productive - at least initially, hard to say about later
#define DYNAMIC_SYMBOL_WEIGHT 0.5


double const DefaultFeatureWeight = 1.0;
double const DefaultFeatureVariance = sqr(1000);





#endif
