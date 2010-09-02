
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
	using std::cerr;
	mt19937 rng(37);
	Eigen::MatrixXd points = DatasetUtils::MakePointCloud(rng,rng,DIMS,1000,2);
	

	BOOST_CHECK(PcaHighDim::Covariance(points).isApprox(PcaHighDim::CovarianceB(points)));
	if( !PcaHighDim::Covariance(points).isApprox(PcaHighDim::CovarianceB(points))) {
		cout << PcaHighDim::Covariance(points).sum() <<"\n";
		cout << PcaHighDim::CovarianceB(points).sum() <<"\n";
		cout << (PcaHighDim::CovarianceB(points) - PcaHighDim::Covariance(points)).sum() <<"\n";
	}
	Eigen::BenchTimer t, tB;
	double ignore=0;
	t.start();
	for(int i=0;i<100;i++) {
		ignore+=PcaHighDim::Covariance(points).sum();
	}
	t.stop();
	cout<< t.total()<<"\n";
	tB.start();
	for(int i=0;i<100;i++) {
		ignore+=PcaHighDim::CovarianceB(points).sum();
	}
	tB.stop();
	cout<< tB.total()<<"\n";
}


BOOST_AUTO_TEST_CASE( pca_vs_svd_test )
{

}