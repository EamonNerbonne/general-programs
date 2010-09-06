#include "stdafx.h"
#include <boost/random/mersenne_twister.hpp>
#include "PCA.h"
#include "DatasetUtils.h"

using namespace Eigen;

BOOST_AUTO_TEST_CASE( pca_vs_svd_test )
{
	using boost::mt19937;
	using std::cout;
	using std::cerr;
	mt19937 rng(1338);
	MatrixXd points = DatasetUtils::MakePointCloud(rng, rng, 7, 1000, 2.12345);

	MatrixXd transform;
	VectorXd eigenvalues;
	PcaHighDim::DoPca(points, transform, eigenvalues);

	MatrixXd newCov = Covariance::ComputeAutoMean( transform * points );

	BOOST_CHECK(newCov.isDiagonal());
	BOOST_CHECK(newCov.diagonal().isApprox(eigenvalues));
	for(ptrdiff_t i=1;i<eigenvalues.size();++i) {
		BOOST_CHECK(eigenvalues(i-1)>= eigenvalues(i));
	}

	BOOST_CHECK(transform.topRows(2).isApprox(PcaProjectInto2d(points)));
}