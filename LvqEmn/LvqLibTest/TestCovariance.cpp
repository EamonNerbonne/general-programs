#include "stdafx.h"
#include <boost/random/mersenne_twister.hpp>
#include "CovarianceAndMean.h"
#include "DatasetUtils.h"



#define DIMS 50
	using boost::mt19937;
	using std::cout;
	using std::cerr;

BOOST_AUTO_TEST_CASE( covariance_test )
{
	typedef CovarianceImpl<MatrixXd> CovHD;
	mt19937 rng(37);
	MatrixXd points = DatasetUtils::MakePointCloud(rng,rng,DIMS,10000,2.3456);
	



	BOOST_CHECK(CovHD::CovarianceA(points).isApprox(CovHD::CovarianceB(points)));
	if( !CovHD::CovarianceA(points).isApprox(CovHD::CovarianceB(points))) {
		cout << CovHD::CovarianceA(points).sum() <<"\n";
		cout << CovHD::CovarianceB(points).sum() <<"\n";
		cout << (CovHD::CovarianceB(points) - CovHD::CovarianceA(points)).sum() <<"\n";
	}
	Eigen::BenchTimer tA, tB, t;
	double ignore=0;
	tA.start();
	for(int i=0;i<100;i++) 
		ignore+=CovHD::CovarianceA(points).sum();
	
	tA.stop();
#ifdef PRINTPERF
	cout<<"CovarianceA duration:"<< tA.total()<<"s\n";
#endif
	tB.start();
	for(int i=0;i<100;i++) 
		ignore+=CovHD::CovarianceB(points).sum();
	
	tB.stop();
#ifdef PRINTPERF
	cout<<"CovarianceB duration:"<< tB.total()<<"s\n";
#endif
	t.start();
	for(int i=0;i<100;i++) 
		ignore+=Covariance::Compute(points).sum();
	
	t.stop();
#ifdef PRINTPERF
	cout<<"Covariance duration:"<< t.total()<<"s ("<<ignore<<")\n";
#endif
	BOOST_CHECK(tA.total()+tB.total() >= 2*t.total());
}


BOOST_AUTO_TEST_CASE( covariance_lowdim_test )
{
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
#ifdef PRINTPERF
	cout<<"LCovarianceA duration:"<< tA.total()<<"s\n";
#endif
	tB.start();
	for(int i=0;i<100;i++) {
		ignore+=CovLD::CovarianceB(points).sum();
	}
	tB.stop();
#ifdef PRINTPERF
	cout<<"LCovarianceB duration:"<< tB.total()<<"s\n";
#endif
		t.start();
	for(int i=0;i<100;i++) {
		ignore+=Covariance::Compute(points).sum();
	}
	t.stop();
#ifdef PRINTPERF
	cout<<"LCovariance duration:"<< t.total()<<"s ("<<ignore<<")\n";
#endif
	BOOST_CHECK(tA.total()+tB.total() >= 2*t.total());
}
