#pragma once
#include <numeric>
class MeanMinMax
{
	double m_count,m_min,m_max,m_sum;
public:

	MeanMinMax()
		: m_min(std::numeric_limits<double>::max())
		, m_max(-std::numeric_limits<double>::max())
		, m_sum(0.0)
		, m_count(0)
	{}

	void Add(double val) {
		m_count++;
		m_sum += val;
		if(val < m_min) m_min = val;
		if(val > m_max) m_max = val;
	}
	double mean() const{return m_sum/m_count;}
	double max() const{return m_max;}
	double min() const{return m_min;}
	double sum() const{return m_sum;}
	double count() const{return m_count;}
};

