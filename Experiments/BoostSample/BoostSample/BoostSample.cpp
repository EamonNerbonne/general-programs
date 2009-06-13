// BoostSample.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

int _tmain(int argc, _TCHAR* argv[])
{
    using namespace boost::lambda;
	typedef std::istream_iterator<int, __wchar_t> in;

    std::for_each(
		in(std::wcin), in(), std::wcout << (_1 * 3) << " " << "\n"  );

	return 0;
}
