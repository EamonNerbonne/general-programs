#pragma once
using namespace System;
#include "stdafx.h"
#include "LvqDataSet.h"
#include "WrappingUtils.h"

namespace LVQCppCli {

	public ref class LvqDataSetCli
	{
		CAutoNativePtr<LvqDataSet> dataset;
		size_t nativeAllocEstimate;
		!LvqDataSetCli();
public:
		LvqDataSetCli(array<double,2>^ points, array<int>^ pointLabels, int classCount);
		LvqDataSet const * GetDataSet() {return dataset;}
	};
}

