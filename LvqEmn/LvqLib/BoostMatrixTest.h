#pragma once

class BoostMatrixTest
{
public:
	BoostMatrixTest(void);
	~BoostMatrixTest(void);
	static void TestMult(int iters, int dims);
	static void TestMultCustom(int iters, int dims);
};
