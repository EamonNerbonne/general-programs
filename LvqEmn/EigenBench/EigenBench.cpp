#include "standard.h"

#include "diagUpdateBench.h"

#include "projectionBench.h"
#include "subtractBench.h"
#include "matmulTest.h"
#include "copyVecTest.h"
#include "prodNormTest.h"
#include "resizeTest.h"
#include "covarience.h"

#include <Eigen/StdVector>
#include <vector>
#include <fstream>
//from http://www.codeproject.com/KB/files/filesize.aspx
int file_size(const char* sFileName)
{
  std::ifstream f;
  f.open(sFileName, std::ios_base::binary | std::ios_base::in);
  if (!f.good() || f.eof() || !f.is_open()) { return 0; }
  f.seekg(0, std::ios_base::beg);
  std::ifstream::pos_type begin_pos = f.tellg();
  f.seekg(0, std::ios_base::end);
  return static_cast<int>(f.tellg() - begin_pos);
}


//class TestIt {
//public:
//	int whatever;
//	Vector2d P_point;
//	EIGEN_MAKE_ALIGNED_OPERATOR_NEW
//};

int main(int , char*argv []){ 
	//std::vector<Vector2d> stlvec;
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
#ifdef _MSC_VER
	cout << " on MSC";
#else
#ifdef __GNUC__
	cout << " on GCC";
#else
	cout << " on ???";
#endif
#endif
	cout<<": ";

	cout <<  file_size(argv[0])/1024 <<"KB\n"; //resizeTest() <<"s; "


	return 	docovbench();

}
