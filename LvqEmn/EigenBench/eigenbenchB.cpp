#include "EigenBench.h"

EIGEN_DONT_INLINE double run_test(
  const Vector2d& mu_vJ, const Vector2d& mu_vK,
  const VectorXd& vJ, const VectorXd& vK,
  const double lr_P,
  Matrix<double,2,Dynamic>& P)
{
#if 0
#if EIGENV2
  //*
  P -=  lr_P * (( mu_vJ * vJ.transpose()).lazy() +( mu_vK * vK.transpose()).lazy() );
  double pNormScale = 1.0 /  (P.transpose() * P).lazy().diagonal().sum();
  /*/
  P -= lr_P * ( mu_vJ * vJ.transpose() + mu_vK * vK.transpose()).lazy();
  double pNormScale = 1.0 /  (P.transpose() * P).diagonal().sum();
  /**/
  return pNormScale;
#else
  P.noalias() -= lr_P * ( mu_vJ * vJ.transpose() + mu_vK * vK.transpose());
  double pNormScale = 1.0 / ( (P.transpose() * P).diagonal().sum());
  return pNormScale;
#endif
#else
	double bla= mu_vK.dot(P * vK);
	return bla;
  //P -=  lr_P * (( mu_vJ * vJ.transpose()).lazy() +( mu_vK * vK.transpose()).lazy() );
  //double pNormScale = 1.0 /  (P.transpose() * P).lazy().diagonal().sum();

#endif
}

void eigentest()
{
  Vector2d mu_vJ = Vector2d::Random();
  Vector2d mu_vK = Vector2d::Random();
  VectorXd vJ = VectorXd::Random(25);
  VectorXd vK = VectorXd::Random(25);
  Matrix<double,2,Dynamic> P = Matrix<double,2,Dynamic>::Random(2,25);
  double lr_P = ei_random<double>();

  progress_timer t;

  const int num_runs = 50000000;

  double sum = 0.0;
  for (int i=0; i<num_runs; ++i) {
	  P(num_runs%2, (num_runs/2)%25) = 1.0;
	  sum += run_test(mu_vJ, mu_vK, vJ, vK, lr_P, P);
  }
  std::cout << sum<<std::endl;
}



int _tmain(int argc, _TCHAR* argv[])
{
	eigentest();

	return 0;
}

