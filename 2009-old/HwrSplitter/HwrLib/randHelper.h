#pragma once
#ifndef RAND_HELPER
#define RAND_HELPER

inline Float FloatRand() {
	unsigned int val;
	rand_s(&val);
	return Float(val)/Float(UINT_MAX);
}

#endif


