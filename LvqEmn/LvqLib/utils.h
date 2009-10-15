#pragma once
#include "stdafx.h"

template <typename T> T sqr(T val) {return val*val;}

template <class T, class S> T sum(T zero, S & container) {	return std::accumulate(container.begin(),container.end(),zero);}

void makeRandomOrder(boost::mt19937 & randGen, std::vector<int> & toFill, int count); 