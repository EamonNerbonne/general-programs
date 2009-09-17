#pragma once
#include "WordSplitSolver.h"
namespace HwrLibCliWrapper {
	public ref class SymbolLearningData
	{
		AllSymbolClasses* symbols;
		!SymbolLearningData();
		~SymbolLearningData();
		int iteration;
	public:

		SymbolLearningData(int symbolCount);
		AllSymbolClasses* DataSink() {return symbols;}
		void Reset() { symbols->resetToZero(); }
		void IncIteration() { iteration++; }
	};
}