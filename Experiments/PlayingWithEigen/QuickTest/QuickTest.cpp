
#include <iostream>
#include <Eigen/Core>
/*
template <int N>
struct S
{
 Eigen::Matrix<double,2*N,2*N> m;

 void F();
};

template <int N>
void S<N>::F()
{
 (m.block<N,N>(0,0)).setIdentity();
 (m.block<N,N>(0,N)).setIdentity();
};

// Here comes a bogus specialization of Eigen::Matrix:
namespace Eigen {
template<>
struct Matrix<double, 8, 8, 0, 8, 8>{
 struct bogus{
   bogus& operator<(int n) { std::cout << "<" << n; return *this;}
   bogus& operator,(bool f) { std::cout << "," << f; return *this;}
   void setIdentity() {std::cout << "setIdentity()\n"; }
 } block;
};
}

int main()
{
 S<4> s;
 s.F();
}
*/

using namespace Eigen;

int main(int, char* []) {
	VectorXd vec = VectorXd::Random(10);
	Vector2d small = Vector2d::Random();
	Matrix<double,2,Dynamic> P = Matrix<double,2,Dynamic>::Random(2,10);

	double scalar = 1.234;



	vec -=   P.transpose() * (scalar * small) ;

}


