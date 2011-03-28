#pragma once
#include "LvqTypedefs.h"

class NearestNeighbor
{
	Matrix_22 transform;
	Matrix_P sortedPoints;
	std::vector<unsigned> idxs;	
	void init();
public:
	template<typename TDerived>
	NearestNeighbor(Eigen::MatrixBase<TDerived> const & points)
		:idxs(points.cols())
		,sortedPoints(points) { init(); }
	int nearestIdx(Vector_2 const & point) const;

	EIGEN_MAKE_ALIGNED_OPERATOR_NEW
};

