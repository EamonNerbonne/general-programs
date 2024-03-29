#include "stdafx.h"
#include "CovarianceAndMean.h"
#include "CreateDataset.h"
#include "LvqTypedefs.h"
#include <iostream>

#define DIMS 17
using std::mt19937;
using std::cout;
using std::cerr;

//#define PRINTPERF 1

#ifdef PRINTPERF
#define BENCHRET(timer,tries,repeat,code) \
    do { \
    BENCH(timer,tries,repeat,code); \
    cout<<"Duration:"<< timer.best()<<"s\n"; \
    } while(false);
#else
#define BENCHRET(timer,tries,repeat,code) \
    BENCH(timer,tries,repeat,code)
#endif
BOOST_AUTO_TEST_CASE(covariance_test)
{
    typedef CovarianceImpl<Matrix_NN> CovHD;
    mt19937 rng(1337);
    Matrix_NN points = CreateDataset::MakePointCloud(rng, rng, DIMS, 1000, 2.3456);


    Vector_N mean = MeanPoint(points);

    BOOST_CHECK(CovHD::CovarianceA(points, mean).isApprox(CovHD::CovarianceB(points, mean)));
    BOOST_CHECK(CovHD::CovarianceA(points, mean).isApprox(CovHD::CovarianceC(points, mean)));
    if (!CovHD::CovarianceA(points, mean).isApprox(CovHD::CovarianceC(points, mean))) {
        cout << CovHD::CovarianceA(points, mean).sum() << "\n";
        cout << CovHD::CovarianceC(points, mean).sum() << "\n";
        cout << (CovHD::CovarianceC(points, mean) - CovHD::CovarianceA(points, mean)).sum() << "\n";
    }
    Eigen::BenchTimer tA, tB, tC, tD, t;
    double ignore = 0;
    BENCHRET(tA, 10, 10, ignore += CovHD::CovarianceA(points, mean).sum());
    BENCHRET(tB, 10, 10, ignore += CovHD::CovarianceB(points, mean).sum());
    BENCHRET(tC, 10, 10, ignore += CovHD::CovarianceC(points, mean).sum());
    BENCHRET(tD, 10, 10, ignore += CovHD::CovarianceD(points, mean).sum());
    BENCHRET(t, 10, 10, ignore += Covariance::Compute<Matrix_NN>(points, mean).sum());
    BOOST_CHECK(t.best() <= 1.05 * std::min(tA.best(), std::min(tB.best(), std::min(tC.best(), tD.best()))));
}


BOOST_AUTO_TEST_CASE(covariance_lowdim_test)
{
    typedef CovarianceImpl<Matrix_P> CovLD;
    mt19937 rng(1337);
    Matrix_P points = CreateDataset::MakePointCloud(rng, rng, LVQ_LOW_DIM_SPACE, 10000, 2.3456);
    Vector_2 mean = MeanPoint(points);

    BOOST_CHECK(CovLD::CovarianceA(points, mean).isApprox(CovLD::CovarianceB(points, mean)));
    BOOST_CHECK(CovLD::CovarianceA(points, mean).isApprox(CovLD::CovarianceC(points, mean)));
    if (!CovLD::CovarianceA(points, mean).isApprox(CovLD::CovarianceC(points, mean))) {
        cout << CovLD::CovarianceA(points, mean) << "\n";
        cout << CovLD::CovarianceC(points, mean) << "\n";
        cout << (CovLD::CovarianceC(points, mean) - CovLD::CovarianceA(points, mean)).sum() << "\n";
    }
    Eigen::BenchTimer tA, tB, tC, tD, t;
    double ignore = 0;
    BENCHRET(tA, 10, 10, ignore += CovLD::CovarianceA(points, mean).sum());
    BENCHRET(tB, 10, 10, ignore += CovLD::CovarianceB(points, mean).sum());
    BENCHRET(tC, 10, 10, ignore += CovLD::CovarianceC(points, mean).sum());
    BENCHRET(tD, 10, 10, ignore += CovLD::CovarianceD(points, mean).sum());
    BENCHRET(t, 10, 10, ignore += Covariance::Compute<Matrix_P>(points, mean).sum());
    using std::cout;
    cout << tA.best() << " " << tB.best() << " " << tC.best() << " " << tD.best() << " " << t.best() << "\n";
    BOOST_CHECK(t.best() <= 1.05 * std::min(tA.best(), std::min(tB.best(), std::min(tC.best(), tD.best()))));
}
