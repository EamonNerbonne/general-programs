// QuickBench.cpp : Defines the entry point for the console application.
//

#include <bench\BenchTimer.h>
#include <vector>
#include <iostream>
#include <numeric>

const int N = 100000000;

struct Point {
	int x;
	int y;
};

using namespace std;
using namespace Eigen;

__declspec(noinline)  Point point_sum(vector<Point> const & points) {
	return std::accumulate(points.begin(), points.end(), Point{ 0, 0 }, [](Point a, Point b) {
		return Point { a.x + b.x, a.y + b.y };
	});
}
__declspec(noinline) Point point_sum2(vector<Point> const & points) {
	Point sum{ 0, 0 };
	for_each(points.begin(), points.end(), [&](Point point) {
		sum.x += point.x;
		sum.y += point.y;
	});
	return sum;
}
__declspec(noinline) Point point_sum3(vector<Point> const & points) {
	Point sum{ 0, 0 };
	for (size_t j = 0; j < points.size(); ++j) {
		sum.x += points[j].x;
		sum.y += points[j].y;
	}
	return sum;
}
typedef Matrix<int, 2, Dynamic> TMat;
typedef Matrix<TMat::Scalar, 2, 1> TVec;

vector<Point> make_point_arr(){
	vector<Point> points(N);
	for (size_t j = 0; j < points.size(); ++j)
		points[j] = Point{ int(j * 2 + 1), int(j * 3 + 2) };
	return points;
}

__declspec(noinline) TVec point_sum_eigen(TMat const & points) {
	return  points.rowwise().sum();
}
TMat make_point_arr_eigen(){
	TMat points(2, N);
	for (TMat::Index j = 0; j < points.cols(); ++j)
		points.col(j) = TVec{ j * 2 + 1, j * 3 + 2 };
	return points;
}

void print_point(TVec & vector) { cout << "(" << vector(0) << ", " << vector(1) << ")\n"; }
void print_point(Point & vector) { cout << "(" << vector.x << ", " << vector.y << ")\n"; }

template<typename TSum, typename TInit> void bench_sum(TInit init_func, TSum sum_func, string name) {
	BenchTimer timer;
	auto points = init_func();
	for (int i = 0; i < 10; ++i) {
		timer.start();
		auto sum = sum_func(points);
		timer.stop();
		//print_point(sum);
	}
	cout << timer.best() / N * 1000 * 1000 * 1000 << "ns / iter; " << name << "\n";
}

#define bench(init, sum) bench_sum(init, sum, #sum)

//#define USE_EIGEN

int main(int argc, char* argv[])
{
	bench(make_point_arr, point_sum);
	bench(make_point_arr, point_sum2);
	bench(make_point_arr, point_sum3);
	bench(make_point_arr_eigen, point_sum_eigen);
}

