#include "StdAfx.h"
#include "CollectionSpeedTestCpp.h"
#include "SSEdotProd.h"
using namespace System;

inline double DotProd(array<double>^ a,array<double>^ b) {
	double sum = 0.0;
	//int max = vecA.Length;
	for (int i = 0; i < CollectionSpeedTestCpp::SIZE; i++)
		sum += a[i] * b[i];
	return sum;
}
inline double DotProd(double* a,double* b) {
	double sum = 0.0;
	//int max = vecA.Length;
	for (int i = 0; i < CollectionSpeedTestCpp::SIZE; i++)
		sum += a[i] * b[i];
	return sum;
}


double CollectionSpeedTestCpp::TestWithDotProd(array<double>^ a,array<double>^ b) {
	pin_ptr<double> aP= &a[0], bP=&b[0];
	double sum=0.0;
	for(int i=0;i<ITER;i++) {
		sum += DotProd(aP,bP);
	}
	return sum;
}
double CollectionSpeedTestCpp::TestWithDotProdExt(array<double>^ a,array<double>^ b) {
	pin_ptr<double> aP= &a[0], bP=&b[0];
	return SSEdotProd::DoTest(aP,bP);
}

double SingleFuncTest(double * a, double *b) {
	double sumO=0.0;
	for(int i=0;i<10000000;i++) {
		double sumI=0.0;
		for(int j=0;j<100;j++)
			sumI+=a[j]*b[j];
		sumO+=sumI;
	}
	return sumO;
}


double CollectionSpeedTestCpp::TestWithDotProdExt2(array<double>^ a,array<double>^ b) {
	pin_ptr<double> aP= &a[0], bP=&b[0];
	return SingleFuncTest(aP,bP);
}



CollectionSpeedTestCpp::CollectionSpeedTestCpp(void){}


// CollectionSpeedTestCpp.cpp : main project file.

int CollectionSpeedTestCpp::main(array<System::String ^> ^args)
{
	Console::WriteLine(L"Hello World");
	return 0;

}

int main(array<System::String ^> ^args) {	CollectionSpeedTestCpp::main(args);}
