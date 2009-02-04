
#include "SSEdotProd.h"


double SSEdotProd::DoTest(double * a, double *b) {
	double sumO=0.0;
	for(int i=0;i<10000000;i++) {
		double sumI=0.0;
		for(int j=0;j<100;j++)
			sumI+=a[j]*b[j];
		sumO+=sumI;
	}
	return sumO;
}

SSEdotProd::SSEdotProd(void)
{
}

SSEdotProd::~SSEdotProd(void)
{
}
