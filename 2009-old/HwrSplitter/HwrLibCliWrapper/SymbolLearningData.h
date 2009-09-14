#pragma once
#include "WordSplitSolver.h"
namespace HwrLibCliWrapper {
	ref class SymbolLearningData
	{
			AllSymbolClasses* symbols;
			!SymbolLearningData();
			~SymbolLearningData();
	public:
		SymbolLearningData(int symbolCount);
	};
}