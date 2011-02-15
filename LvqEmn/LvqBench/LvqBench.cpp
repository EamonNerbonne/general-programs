#include "stdafx.h"
#include "EasyLvqTest.h"
#include <fstream>
#include "G2mLvqModel.h"
#include "GsmLvqModel.h"
#include "LgmLvqModel.h"


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

int main(int , char*argv []){ 
	using std::cout;
	cout<<"LvqBench";
#ifdef EIGEN_DONT_VECTORIZE
	cout<< "NV";
#else
	cout<< "V";
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
	std::cerr<<" ("<<sizeof(G2mLvqModel) <<") ";
	std::cerr<<" ("<<sizeof(GsmLvqModel) <<") ";
	std::cerr<<" ("<<sizeof(LgmLvqModel) <<") ";
	Eigen::BenchTimer t;
	t.start();
	EasyLvqTest();
	t.stop();
	cout<<"; "<< file_size(argv[0])/1024 <<"KB\n";
	std::cerr<<"Total time:"<<t.value()<<"s\n";

	return 0;
}
