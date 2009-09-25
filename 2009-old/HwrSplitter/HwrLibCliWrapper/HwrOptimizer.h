// HwrLibCliWrapper.h

#pragma once
#include "Stdafx.h"
#pragma warning (disable:4482)
#include "ImageStruct.h"
#include "ImageProcessor.h"

#include "WordSplitSolver.h"
#include "SymbolLearningData.h"

namespace HwrLibCliWrapper {
	public ref class HwrOptimizer {
		msclr::auto_handle<SymbolLearningData> nativeSymbols;
		HwrDataModel::SymbolClasses^ managedSymbols;
	public:
		HwrOptimizer(HwrDataModel::SymbolClasses^ symbolClasses) : nativeSymbols(gcnew SymbolLearningData(symbolClasses->Count )) {
			managedSymbols = symbolClasses;
			nativeSymbols->Randomize(); //this is important; if symbolClasses are uninitialized (i.e. zero) then variances are zero, and no estimate and learning can occur.
			nativeSymbols->LoadFromManaged(managedSymbols);
		}

		void SaveToManaged() { nativeSymbols->SaveToManaged(managedSymbols); }
		property HwrDataModel::SymbolClasses^ ManagedSymbols { HwrDataModel::SymbolClasses^ get() {return managedSymbols;}}

		SymbolLearningData^ ConstructLearningCache() {return gcnew SymbolLearningData(nativeSymbols->GetSymbols()->size());}

		void SplitWords(ImageStruct<signed char> block, int cropXoffset, HwrDataModel::HwrTextLine^ textLine, SymbolLearningData ^ dataSink);

		void MergeInLearningCache(SymbolLearningData^ learningCache) {nativeSymbols->MergeInLearningCache(learningCache); managedSymbols->Iteration = nativeSymbols->GetSymbols()->iteration; }
	};
}

