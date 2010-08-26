
#include <boost/random/mersenne_twister.hpp>
#include "PCA.h"
#include "DatasetUtils.h"


#define BOOST_TEST_INCLUDED
#include <boost/test/unit_test.hpp>

#define DIMS 7

BOOST_AUTO_TEST_CASE( covariance_test )
{
	using boost::mt19937;
	using std::cout;
	mt19937 rng(37);
	Eigen::MatrixXd points = DatasetUtils::MakePointCloud(rng,rng,DIMS,1000,2);
	

	BOOST_CHECK(PrincipalComponentAnalysis::Covariance(points).isApprox(PrincipalComponentAnalysis::CovarianceB(points)));
	if(true|| !PrincipalComponentAnalysis::Covariance(points).isApprox(PrincipalComponentAnalysis::CovarianceB(points))) {
		cout << PrincipalComponentAnalysis::Covariance(points).sum() <<"\n";
		cout << PrincipalComponentAnalysis::CovarianceB(points).sum() <<"\n";
		MatrixXd target;
		PrincipalComponentAnalysis::CovarianceInto(points,target);
		cout << target.sum() <<"\n";
		cout << (PrincipalComponentAnalysis::CovarianceB(points) - PrincipalComponentAnalysis::Covariance(points)).sum() <<"\n";
	}
	Eigen::BenchTimer t, tB,tI;
	double ignore=0;
	t.start();
	for(int i=0;i<100;i++) {
		ignore+=PrincipalComponentAnalysis::Covariance(points).sum();
	}
	t.stop();
	std::cout<< t.total()<<"\n";
	tB.start();
	for(int i=0;i<100;i++) {
		ignore+=PrincipalComponentAnalysis::CovarianceB(points).sum();
	}
	tB.stop();
	std::cout<< tB.total()<<"\n";
	tI.start();
	MatrixXd target;
	for(int i=0;i<100;i++) {
		
		PrincipalComponentAnalysis::CovarianceInto(points,target);
		ignore+=target.sum();
	}
	tI.stop();
	std::cout<< tI.total()<<"\n";
}
