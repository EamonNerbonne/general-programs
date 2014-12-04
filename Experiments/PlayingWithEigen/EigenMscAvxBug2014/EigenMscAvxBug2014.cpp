#include <iostream>
#include <memory>

#define EIGEN_NO_AUTOMATIC_RESIZING
#define _USE_MATH_DEFINES

#include <Eigen/Core>

#pragma warning(disable: 4714)

using namespace std;
using namespace Eigen;

struct Wrapper {
	MatrixXd data;

	__declspec(noinline) Wrapper(MatrixXd const & points) : data(points){
		cout << data.rowwise().mean() << "\n\n";
		cout << points.rowwise().mean() << "\n\n";

	}
};



MatrixXd func(MatrixXd const & input) {
	MatrixXd local(input);
	return local;
}

int main(int , wchar_t* [])
{
	MatrixXd sample = VectorXd::LinSpaced(6, 1, 6) * VectorXd::LinSpaced(12, 1, 12).transpose();
	cout << sample.rowwise().mean() << "\n\n";
	unique_ptr<Wrapper> ptr(new Wrapper(sample));
	cout << ptr->data.rowwise().mean() << "\n\n";
	//for (size_t i = 0; i < sample.cols(); ++i)
	//	output.col(i) = sample.col(i);

	cout << sample <<"\n\n";
	cout << ptr->data << "\n\n";
	return 0;
}

