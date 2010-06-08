#pragma once
namespace LvqLibCli {
	public value class PointSet sealed {
	public:
		array<double,2>^ Points;
		array<int>^ ClassLabels;
	};

	public value class ModelProjection sealed {
	public:
		PointSet Data;
		PointSet Prototypes;
		bool IsOk;
	};
}