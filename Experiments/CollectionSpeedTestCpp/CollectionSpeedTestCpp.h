#pragma once

public ref class CollectionSpeedTestCpp
{
public:
    static const int ITER = 10000000;
    static const int SIZE = 100;
	CollectionSpeedTestCpp(void);
	static int main(array<System::String^>^args);

	static double TestWithDotProd(array<double>^ a, array<double>^ b);
	static double TestWithDotProdExt(array<double>^ a, array<double>^ b);
	static double TestWithDotProdExt2(array<double>^ a, array<double>^ b);
};
