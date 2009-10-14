#pragma once
using namespace System;
#include "stdafx.h"
#include "LvqDataSet.h"

namespace LVQCppCli {

	public ref class LvqWrapper
	{
		CAutoNativePtr<LvqDataSet> dataset;
		CAutoNativePtr<LvqModel> model;
	public:
		LvqWrapper(array<double,2>^ points, array<int>^ pointLabels, int classCount, int protosPerDistrib);

		double Evaluate();
	};
}
