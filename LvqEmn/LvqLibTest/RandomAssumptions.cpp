#include "stdafx.h"
#include <boost/random/mersenne_twister.hpp>
using boost::mt19937;


struct container {
	container(mt19937 & rng) : rnd(rng){}
	mt19937 rnd;
	unsigned operator()(){ return rnd();	}
};

BOOST_AUTO_TEST_CASE( random_test )
{
	mt19937 rnd1(37), rnd2(37);
	BOOST_CHECK(rnd1 == rnd2);
	BOOST_CHECK_EQUAL(rnd1(), rnd2());

	mt19937 rnd3(rnd1);
	BOOST_CHECK(rnd1 == rnd3);
	BOOST_CHECK_EQUAL(rnd1(), rnd3()); rnd2();

	container x(rnd2);

	BOOST_CHECK_EQUAL(x(), rnd3());
	BOOST_CHECK_EQUAL(rnd1(), rnd2());

	mt19937 rndA(x), rndB(x);

	BOOST_CHECK_NE(rndA(), rndB());
	

}