#pragma warning(disable:4820)
#pragma warning(disable:4986)
#pragma warning(disable:4626)
#pragma warning(disable:4365)
#pragma warning(disable:4514)
#pragma warning(disable:4710)
#include <iostream>
#include <Eigen/Core>

using namespace Eigen;

int main(int, char* []) {
	VectorXd vec = VectorXd::Random(10);
	Vector2d small = Vector2d::Random();
	Matrix<double,2,Dynamic> P = Matrix<double,2,Dynamic>::Random(2,10);
	Matrix<double,Dynamic,2> Pt = P.transpose();

	double scalar = 1.234;

	//vec = Pt*small;
	//vec = Pt*(scalar*small);
	//vec = scalar*Pt*small;

	//vec -= Pt*small;
	//vec -= Pt*(scalar*small);
	//vec -= scalar*Pt*small;

	//vec = P.transpose()*small;
	//vec = P.transpose()*(scalar*small);
	//vec = scalar*P.transpose()*small;

	//vec -= P.transpose()*small;
	//vec -= P.transpose()*(scalar*small);
	//vec -= scalar*P.transpose()*small;

	vec.noalias() = Pt*small;
	vec.noalias() = Pt*(scalar*small);
	vec.noalias() = scalar*Pt*small;

	vec.noalias() -= Pt*small;
	vec.noalias() -= Pt*(scalar*small);
	vec.noalias() -= scalar*Pt*small;

	vec.noalias() = P.transpose()*small;
	vec.noalias() = P.transpose()*(scalar*small);
	vec.noalias() = scalar*P.transpose()*small;

	vec.noalias() -= P.transpose()*small;
	vec.noalias() -= P.transpose()*(scalar*small);
	vec.noalias() -= scalar*P.transpose()*small;

}


