
#include <boost/random/mersenne_twister.hpp>
#include "CovarianceAndMean.h"
#include "DatasetUtils.h"


#define BOOST_TEST_INCLUDED
#include <boost/test/unit_test.hpp>

#define DIMS 5

using namespace Eigen;

BOOST_AUTO_TEST_CASE( covariance_test )
{
	using boost::mt19937;
	using std::cout;
	using std::cerr;
	typedef CovarianceImpl<MatrixXd> CovHD;
	mt19937 rng(37);
	MatrixXd points = DatasetUtils::MakePointCloud(rng,rng,DIMS,10000,2.3456);
	



	BOOST_CHECK(CovHD::CovarianceA(points).isApprox(CovHD::CovarianceB(points)));
	if( !CovHD::CovarianceA(points).isApprox(CovHD::CovarianceB(points))) {
		cout << CovHD::CovarianceA(points).sum() <<"\n";
		cout << CovHD::CovarianceB(points).sum() <<"\n";
		cout << (CovHD::CovarianceB(points) - CovHD::CovarianceA(points)).sum() <<"\n";
	}
	Eigen::BenchTimer tA, tB,t;
	double ignore=0;
	tA.start();
	for(int i=0;i<100;i++) {
		ignore+=CovHD::CovarianceA(points).sum();
	}
	tA.stop();
	cout<<"CovarianceA duration:"<< tA.total()<<"s\n";
	tB.start();
	for(int i=0;i<100;i++) {
		ignore+=CovHD::CovarianceB(points).sum();
	}
	tB.stop();
	cout<<"CovarianceB duration:"<< tB.total()<<"s\n";
	t.start();
	for(int i=0;i<100;i++) {
		ignore+=Covariance::Compute(points).sum();
	}
	t.stop();
	cout<<"Covariance duration:"<< t.total()<<"s ("<<ignore<<")\n";

}


BOOST_AUTO_TEST_CASE( covariance_lowdim_test )
{
	using boost::mt19937;
	using std::cout;
	using std::cerr;
	typedef CovarianceImpl<PMatrix> CovLD;
	mt19937 rng(37);
	PMatrix points = DatasetUtils::MakePointCloud(rng,rng,LVQ_LOW_DIM_SPACE,10000,2.3456);
	

	BOOST_CHECK(CovLD::CovarianceA(points).isApprox(CovLD::CovarianceB(points)));
	if( !CovLD::CovarianceA(points).isApprox(CovLD::CovarianceB(points))) {
		cout << CovLD::CovarianceA(points).sum() <<"\n";
		cout << CovLD::CovarianceB(points).sum() <<"\n";
		cout << (CovLD::CovarianceB(points) - CovLD::CovarianceA(points)).sum() <<"\n";
	}
	Eigen::BenchTimer tA, tB,t;
	double ignore=0;
	tA.start();
	for(int i=0;i<100;i++) {
		ignore+=CovLD::CovarianceA(points).sum();
	}
	tA.stop();
	cout<<"LCovarianceA duration:"<< tA.total()<<"s\n";
	tB.start();
	for(int i=0;i<100;i++) {
		ignore+=CovLD::CovarianceB(points).sum();
	}
	tB.stop();
	cout<<"LCovarianceB duration:"<< tB.total()<<"s\n";
		t.start();
	for(int i=0;i<100;i++) {
		ignore+=Covariance::Compute(points).sum();
	}
	t.stop();
	cout<<"LCovariance duration:"<< t.total()<<"s ("<<ignore<<")\n";

}

