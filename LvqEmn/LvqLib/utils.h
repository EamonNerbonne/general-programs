#pragma once
#include "stdafx.h"

template <typename T> T sqr(T val) {return val*val;}

void makeRandomOrder(boost::mt19937 & randGen, int*const toFill, int count);
