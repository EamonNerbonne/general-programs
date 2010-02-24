#include <boost/progress.hpp>
#include <Eigen/Core>
#if !EIGEN3
#include <Eigen/Array>
#endif

using namespace Eigen;
using namespace boost;
using namespace std;

void mulBench(void);
void learningBench(void);

double run_test(
		const Vector2d& mu_vJ, const Vector2d& mu_vK,
		const VectorXd& vJ, const VectorXd& vK,
		const double lr_P,
		Matrix<double,2,Dynamic>& P) {
#if EIGEN3
	P.noalias() -= lr_P * ( mu_vJ * vJ.transpose() + mu_vK * vK.transpose());
	return 1.0 / ( (P.transpose() * P).diagonal().sum());
#else
	P = P-  lr_P * (( mu_vJ * vJ.transpose()).lazy() +( mu_vK * vK.transpose()).lazy() );
	return 1.0 /  (P.transpose() * P).lazy().diagonal().sum();
#endif
}

void learningBench() {
	Vector2d mu_vJ = Vector2d::Random();
	Vector2d mu_vK = Vector2d::Random();
	VectorXd vJ = VectorXd::Random(25);
	VectorXd vK = VectorXd::Random(25);
	Matrix<double,2,Dynamic> P = Matrix<double,2,Dynamic>::Random(2,25);
	double lr_P = ei_random<double>();

	progress_timer t;
	double sum = 0.0;
	const int num_runs = 10000000;
	for (int i=0; i<num_runs; ++i) {
		P(num_runs%2, (num_runs/2)%25) = 1.0;
		sum += run_test(mu_vJ, mu_vK, vJ, vK, lr_P, P);
	}
	cout <<"(" << sum<<") ";
}

void mulBench(void) {
	using namespace std;

	Vector2d mu_vK = Vector2d::Random();
	VectorXd vK = VectorXd::Random(25);
	Matrix<double,2,Dynamic> P = Matrix<double,2,Dynamic>::Random(2,25);
	const int num_runs = 50000000;
	double sum = 0.0;

	progress_timer t;
	for (int i=0; i<num_runs; ++i) {
		P(num_runs%2, (num_runs/2)%25) = 1.0; //vs. optimizer
		sum +=  mu_vK.dot(P * vK);
	}
	cout << sum<<endl;//vs. optimizer
}

int main(int argc, char* argv[]){ 
    //mulBench(); 
	cout<<"EigenBench";
#if EIGEN3
	cout<< "3";
#else
#if EIGEN2
	cout<< "2";
#else
	cout<<"????";
#endif
#endif
#ifndef EIGEN_DONT_VECTORIZE
	cout<< "v";
#endif
#ifndef NDEBUG
	cout<< "[DEBUG]";
#endif
	cout<<": ";
	learningBench();
    return 0; 
}
