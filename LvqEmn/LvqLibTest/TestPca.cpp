#include <boost/random/mersenne_twister.hpp>
#include "PCA.h"
#include "DatasetUtils.h"


#define BOOST_TEST_INCLUDED
#include <boost/test/unit_test.hpp>


BOOST_AUTO_TEST_CASE( pca_vs_svd_test )
{
	using boost::mt19937;
	using std::cout;
	using std::cerr;
	mt19937 rng(1338);
	MatrixXd points = DatasetUtils::MakePointCloud(rng, rng, 5, 1000, 2.12345);

	MatrixXd transform;
	VectorXd eigenvalues;
	PcaHighDim::DoPca(points, transform, eigenvalues);

	MatrixXd newCov = Covariance::Compute (transform * points);

	BOOST_CHECK(newCov.isDiagonal());
	BOOST_CHECK(newCov.diagonal().isApprox(eigenvalues));
}