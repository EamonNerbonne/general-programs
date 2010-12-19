#pragma once
#include "LvqTypedefs.h"

class NearestNeighbor
{
	Eigen::Matrix2d transform;
	PMatrix sortedPoints;
	std::vector<unsigned> idxs;	
	void init();
public:
	template<typename TDerived>
	NearestNeighbor(Eigen::MatrixBase<TDerived> const & points)
		:idxs(points.cols())
		,sortedPoints(points) { init(); }
	int nearestIdx(Eigen::Vector2d const & point) const;

	EIGEN_MAKE_ALIGNED_OPERATOR_NEW
};

