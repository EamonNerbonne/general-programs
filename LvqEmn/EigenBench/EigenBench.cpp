#include <boost/progress.hpp>
#include <Eigen/Core>
#include <Eigen/Array>

using namespace Eigen;

int main(int argc, char* argv[])
{
  Vector2d mu_vK = Vector2d::Random();
  VectorXd vK = VectorXd::Random(25);
  Matrix<double,2,Dynamic> P = Matrix<double,2,Dynamic>::Random(2,25);
  const int num_runs = 50000000;
  double sum = 0.0;

  boost::progress_timer t;
  for (int i=0; i<num_runs; ++i) {
    P(num_runs%2, (num_runs/2)%25) = 1.0; //vs. optimizer
    sum +=  mu_vK.dot(P * vK);
  }
  std::cout << sum<<std::endl;//vs. optimizer
  return 0;
}

