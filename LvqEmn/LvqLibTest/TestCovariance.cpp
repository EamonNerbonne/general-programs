
#include <boost/random/mersenne_twister.hpp>
#include "PCA.h"
#include "DatasetUtils.h"


#define BOOST_TEST_INCLUDED
#include <boost/test/unit_test.hpp>
#include <Eigen/SVD>

#define DIMS 5

using namespace Eigen;

BOOST_AUTO_TEST_CASE( covariance_test )
{
	using boost::mt19937;
	using std::cout;
	using std::cerr;
	mt19937 rng(37);
	MatrixXd points = DatasetUtils::MakePointCloud(rng,rng,DIMS,10000,2.3456);
	

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
	cout<<"Covariance duration:"<< t.total()<<"s\n";
	tB.start();
	for(int i=0;i<100;i++) {
		ignore+=PcaHighDim::CovarianceB(points).sum();
	}
	tB.stop();
	cout<<"CovarianceB duration:"<< tB.total()<<"s\n";
}


BOOST_AUTO_TEST_CASE( covariance_lowdim_test )
{
	using boost::mt19937;
	using std::cout;
	using std::cerr;
	mt19937 rng(37);
	PMatrix points = DatasetUtils::MakePointCloud(rng,rng,LVQ_LOW_DIM_SPACE,10000,2.3456);
	

	BOOST_CHECK(PcaLowDim::Covariance(points).isApprox(PcaLowDim::CovarianceB(points)));
	if( !PcaLowDim::Covariance(points).isApprox(PcaLowDim::CovarianceB(points))) {
		cout << PcaLowDim::Covariance(points).sum() <<"\n";
		cout << PcaLowDim::CovarianceB(points).sum() <<"\n";
		cout << (PcaLowDim::CovarianceB(points) - PcaLowDim::Covariance(points)).sum() <<"\n";
	}
	Eigen::BenchTimer t, tB;
	double ignore=0;
	t.start();
	for(int i=0;i<100;i++) {
		ignore+=PcaLowDim::Covariance(points).sum();
	}
	t.stop();
	cout<<"LCovariance duration:"<< t.total()<<"s\n";
	tB.start();
	for(int i=0;i<100;i++) {
		ignore+=PcaLowDim::CovarianceB(points).sum();
	}
	tB.stop();
	cout<<"LCovarianceB duration:"<< tB.total()<<"s\n";
}



BOOST_AUTO_TEST_CASE( pca_vs_svd_test )
{
	using boost::mt19937;
	using std::cout;
	using std::cerr;
	mt19937 rng(1338);
	MatrixXd points = DatasetUtils::MakePointCloud(rng,rng,DIMS,1000,2);

	MatrixXd transform;
	VectorXd eigenvalues;
	PcaHighDim::DoPca(points, transform, eigenvalues);

	MatrixXd newCov = PcaHighDim::Covariance(transform * points);

	BOOST_CHECK(newCov.isDiagonal());
	cout<<eigenvalues<< "\n";
	cout<<newCov<<"\n";

}