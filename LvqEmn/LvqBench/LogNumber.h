#pragma once
#include <math.h>
using namespace std;

static double ln2 = log(2.0);

typedef double Float;

// 1/(log(2))
//-x/(2 log^2(2))
//+(x^2 (2-log(2)))/(8 log^3(2))
//+(x^3 (log(2)-1))/(8 log^4(2))
//+(x^4 (12+log^3(2)+3 log^2(2)-18 log(2)))/(192 log^5(2))
//+(x^5 (-12-2 log^3(2)-9 log^2(2)+24 log(2)))/(384 log^6(2))

static double c0 =1.0/ ln2;
static double c1 =1.0/ (2.0*ln2*ln2);
static double c2 =(2.0-ln2)/ (8.0*ln2*ln2*ln2);
static double c3 =(ln2-1.0)/ (8.0*ln2*ln2*ln2*ln2);
static double c4 =(12.0 + ln2*ln2*ln2 + 3*ln2*ln2 + 18*ln2) / (192*ln2*ln2*ln2*ln2*ln2);

//1.09596-0.197642 x+0.0396948 x^2-0.00365426 x^3
const double d0 = 1.09596;
const double d1 = - 0.197642;
const double d2 = 0.0396948;
const double d3 = - 0.00365426;





class LogNumber
{
public:
	double exponent;
	static inline LogNumber FromExp(double expon) { LogNumber a; a.exponent=expon; return a;}
	inline LogNumber operator*(LogNumber b) {	return FromExp(exponent+b.exponent); }
	inline LogNumber operator*(double b) {	return FromExp(exponent+std::log(b)); }
	inline LogNumber operator/(LogNumber b) {	return FromExp(exponent-b.exponent); }
	inline bool operator<( LogNumber b) {	return exponent<b.exponent; }
	inline bool operator>( LogNumber b) {	return exponent>b.exponent; }
	inline bool operator<=( LogNumber b) {	return exponent<=b.exponent; }
	inline bool operator>=( LogNumber b) {	return exponent>=b.exponent; }
	inline bool operator==( LogNumber b) {	return exponent==b.exponent; }
	inline bool operator!=(LogNumber b) {	return exponent!=b.exponent; }
	inline LogNumber AddSmaller(LogNumber b) {
		//return  FromExp(exponent+std::log(1+std::exp(b.exponent-exponent) )); 
		//to avoid log+exp costs, we use the taylor expansion of ln(1+e^x);
		double x = b.exponent - exponent;
		double x2 = x*x;
		double x4 = x2*x2;
		double x6 = x4*x2;
		double x8 = x4*x4;
		return FromExp(exponent+std::max(0.0, ln2 + 0.5*x + (1.0/8.0)*x2 - (1.0/192.0)*x4 /* + (1.0/2880.0)*x6 - (17.0/645120.0)*x8*/ ));
	}
	inline LogNumber AddSmaller1(LogNumber b) {
		//just do max
		return FromExp(std::max(exponent, b.exponent ));
	}
	inline LogNumber AddSmaller2(LogNumber b) {
		//return  FromExp(exponent+std::log(1+std::exp(b.exponent-exponent) )); 
		//to avoid log+exp costs, we use the taylor expansion of ln(1+e^x);
		double x = b.exponent - exponent;
		double x2 = x*x;
		double x3 = x2*x;
		double x4 = x2*x2;
		double denom=c0 + c1*x + c2*x2 + c3*x3 + c4*x4;
		return FromExp(exponent+1.0/denom);
	}
	inline LogNumber AddSmaller3(LogNumber b) {
		//to avoid log+exp costs, we use the taylor expansion of ln(1+e^x)^(-1/4);
		double x = b.exponent - exponent;
		double x2 = x*x;
		double x3 = x2*x;
		double lroot=d0 + d1*x + d2*x2 + d3*x3;
		return FromExp(exponent+1.0/(lroot*lroot*lroot*lroot));
	}
	inline LogNumber AddSmallerSlow(LogNumber b) {
		return FromExp(exponent+std::log(1+std::exp(b.exponent-exponent) )); 
	}
	inline LogNumber operator+(LogNumber b) { return exponent>=b.exponent?AddSmaller(b):b.AddSmaller(*this) ; }
	inline LogNumber& operator+=(LogNumber b) { exponent = (*this+b).exponent; return *this; }

	//explicit inline operator double() { return std::exp( exponent); }

	explicit LogNumber(Float val) {
		if(val==0.0)
			exponent = -std::numeric_limits<Float>::max();
		else
			exponent = std::log(val);
	}	
	LogNumber() :exponent(1.0){}
};

inline double ToDouble(LogNumber num) { return std::exp( num.exponent); }
inline double ToDouble(double num) { return num; }
//inline LogNumber FromExp(Float exponent) {return LogNumber::FromExp(exponent);}
//inline double FromExp(Float exponent) {return exp(exponent);}

inline LogNumber exp(LogNumber num,double power) { return LogNumber::FromExp(num.exponent*power);}
