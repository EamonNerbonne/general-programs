#include "stdafx.h"
#include <random>
#include "CreateDataset.h"
#include "utils.h"

using namespace Eigen;
using std::mt19937;
using std::cout;
using std::cerr;


template <typename T> EIGEN_STRONG_INLINE static double projectionSquareNorm(T const & projectionMatrix) {
    auto projectionSquareExpression = projectionMatrix.transpose() * projectionMatrix;
    return projectionSquareExpression.diagonal().sum();
}


BOOST_AUTO_TEST_CASE( normtest )
{
    mt19937 rng(1338);
    Matrix_P points = CreateDataset::MakePointCloud(rng, rng, 2, 30, 2.12345);

    BOOST_CHECK(fabs(projectionSquareNorm(points) - 1.0) > 0.01);

    points *= LvqFloat(1.0/ std::sqrt( projectionSquareNorm(points)));
    BOOST_CHECK(fabs(projectionSquareNorm(points) - 1.0) < 0.01);

}