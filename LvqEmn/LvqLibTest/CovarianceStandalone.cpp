#include <bench/BenchTimer.h>
#include <Eigen/Core>
using namespace Eigen;

static MatrixXd Covariance(Eigen::MatrixBase<MatrixXd> const & points, VectorXd const & mean) {
	return (points.colwise() - mean) * (points.colwise() - mean).transpose() * (1.0/(points.cols()-1.0)) ;
}

static MatrixXd CovarianceB(Eigen::MatrixBase<MatrixXd>const & points, VectorXd const & mean) {
	VectorXd diff = VectorXd::Zero(points.rows());
	MatrixXd cov = MatrixXd::Zero(points.rows(),points.rows());
	for(int i=0;i<points.cols();++i) {
		diff.noalias() = points.col(i) - mean;
		cov.noalias() += diff * diff.transpose();
	}
	return cov * (1.0/(points.cols()-1.0));
}

static MatrixXd CovarianceC(Eigen::MatrixBase<MatrixXd> const & points, VectorXd const & mean) {
	return (points.colwise() - mean) * (1.0/(points.cols()-1.0)) * (points.colwise() - mean).transpose() ;
}


bool testIt() {
	MatrixXd points = Eigen::MatrixXd::Random(7,1001);
	VectorXd mean = points.rowwise().sum() * (1.0/points.cols());
	return Covariance(points,mean).isApprox(CovarianceB(points,mean));
}

bool testIt2() {
	MatrixXd points = Eigen::MatrixXd::Random(7,1001);
	VectorXd mean = points.rowwise().sum() * (1.0/points.cols());
	return Covariance(points,mean).isApprox(CovarianceC(points,mean));
}

bool testIt3() {
	MatrixXd points = Eigen::MatrixXd::Random(7,1001);
	VectorXd mean = points.rowwise().sum() * (1.0/points.cols());
	return CovarianceB(points,mean).isApprox(CovarianceC(points,mean));
}



#ifndef STANDALONE

#define BOOST_TEST_INCLUDED
#include <boost/test/unit_test.hpp>

BOOST_AUTO_TEST_CASE( covariance_standalone_test ){ 
	BOOST_CHECK(testIt());
	BOOST_CHECK(testIt2());
	BOOST_CHECK(testIt3());
}

#else
#include <iostream>
int main(int argc, char *argv[] ) {
	bool isOk = testIt();
	bool isOk2 = testIt2();
	bool isOk3 = testIt3();
	std::cout<<"testIt(): "<< isOk<<"\n";
	std::cout<<"testIt2():"<< isOk2<<"\n";
	std::cout<<"testIt3():"<< isOk3<<"\n";
	if(!isOk || !isOk2 || !isOk3) {
		std::cout << "Bad output!\n";
		return 1;
	}

	return 0;
}

#endif
