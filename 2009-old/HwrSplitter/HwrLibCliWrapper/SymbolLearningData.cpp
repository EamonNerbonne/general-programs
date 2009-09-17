#include "StdAfx.h"
#include "SymbolLearningData.h"

namespace HwrLibCliWrapper {
	SymbolLearningData::!SymbolLearningData() {
		if(symbols != NULL) {
			GC::RemoveMemoryPressure(symbols->AllocatedSize());
			delete symbols;
			symbols = NULL;
		}
	}
	SymbolLearningData::~SymbolLearningData() { this->!SymbolLearningData(); }
	SymbolLearningData::SymbolLearningData(int symbolCount) 
		: symbols(new AllSymbolClasses(symbolCount ))
		, iteration(0)
	{
		GC::AddMemoryPressure(symbols->AllocatedSize());
	}
}