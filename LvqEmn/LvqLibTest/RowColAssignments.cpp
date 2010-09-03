
#include <Eigen/Core>
using namespace Eigen;

bool rowFromCol() {
	MatrixXd matA, matB;
	
	matA = MatrixXd::Random(12,15);
	matB.resize(matA.cols(),matA.rows());
	for(ptrdiff_t i=0;i<matA.cols();++i) 
		matB.row(i) = matA.col(i);
	return matB.isApprox(matA.transpose());
}

bool colFromRow() {
	MatrixXd matA, matB;
	
	matA = MatrixXd::Random(12,15);
	matB.resize(matA.cols(),matA.rows());
	for(ptrdiff_t i=0;i<matA.rows();++i) 
		matB.col(i) = matA.row(i);
	return matB.isApprox(matA.transpose());
}

bool rowFromColNoalias() {
	MatrixXd matA, matB;
	
	matA = MatrixXd::Random(12,15);
	matB.resize(matA.cols(),matA.rows());
	for(ptrdiff_t i=0;i<matA.cols();++i) 
		matB.row(i).noalias() = matA.col(i);
	return matB.isApprox(matA.transpose());
}

bool colFromRowNoalias() {
	MatrixXd matA, matB;
	
	matA = MatrixXd::Random(12,15);
	matB.resize(matA.cols(),matA.rows());
	for(ptrdiff_t i=0;i<matA.rows();++i) 
		matB.col(i).noalias() = matA.row(i);
	return matB.isApprox(matA.transpose());
}


#ifndef STANDALONE
#define BOOST_TEST_INCLUDED
#include <boost/test/unit_test.hpp>

BOOST_AUTO_TEST_CASE( rowColMixedTest )
{
	BOOST_CHECK(rowFromCol());
	BOOST_CHECK(rowFromColNoalias());
	BOOST_CHECK(colFromRow());
	BOOST_CHECK(colFromRowNoalias());
}
#else
#include <iostream>
static bool failed = false;
#define ASSTRING(X) #X
#define DBG(X) do {bool ok=(X);failed |= !ok; std::cout<<ASSTRING(X)<<":\n"<<ok<<"\n"; } while(false)

int main(int argc, char *argv[] ) {
	DBG(rowFromCol());
	DBG(rowFromColNoalias());
	DBG(colFromRow());
	DBG(colFromRowNoalias());

	return failed;
}


#endif