#pragma once
#ifndef RAND_HELPER
#define RAND_HELPER

inline double FloatRand() {
	unsigned int val;
	rand_s(&val);
	return double(val)/double(UINT_MAX);
}

#endif


