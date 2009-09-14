#pragma once
#include <math.h>

static double ln2 = std::log(2.0);

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
	inline LogNumber AddSmallerQuick(LogNumber b) {
		//return  FromExp(exponent+std::log(1+std::exp(b.exponent-exponent) )); 
		//to avoid log+exp costs, we use the taylor expansion of ln(1+e^x);
		double x = b.exponent - exponent;
		double x2 = x*x;
		return FromExp(exponent+std::max(0.0, ln2 + 0.5*x + (1.0/8.0)*x2 - (1.0/192.0)*x2*x2));
	}
	inline LogNumber AddSmaller(LogNumber b) {
		return  FromExp(exponent+std::log(1+std::exp(b.exponent-exponent) )); 
	}
	inline LogNumber operator+(LogNumber b) { return exponent>=b.exponent?AddSmaller(b):b.AddSmaller(*this) ; }
	inline LogNumber& operator+=(LogNumber b) { exponent = (*this+b).exponent; return *this; }

	//explicit inline operator double() { return std::exp( exponent); }

	explicit LogNumber(double val) {
		if(val==0.0)
			exponent = -std::numeric_limits<double>::max();
		else
			exponent = std::log(val);
	}	
	LogNumber() :exponent(1.0){}
};

inline double ToDouble(LogNumber num) { return std::exp( num.exponent); }
inline double ToDouble(double num) { return num; }
//inline LogNumber FromExp(double exponent) {return LogNumber::FromExp(exponent);}
//inline double FromExp(double exponent) {return exp(exponent);}

inline LogNumber exp(LogNumber num,double power) { 	return LogNumber::FromExp(num.exponent*power);}
