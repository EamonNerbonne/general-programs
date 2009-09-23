#pragma once
#include "WordSplitSolver.h"
namespace HwrLibCliWrapper {
	public ref class SymbolLearningData
	{
		AllSymbolClasses* symbols;
		!SymbolLearningData();
		~SymbolLearningData();

		static void CopyToNative(HwrDataModel::SymbolClass ^ managedSymbol, SymbolClass & nativeSymbol);
		static void CopyToManaged(SymbolClass const & nativeSymbol, HwrDataModel::SymbolClass^ managedSymbol);
	public:

		AllSymbolClasses* GetSymbols() {return symbols;}
		void Reset() { symbols->resetToZero(); }
		void Randomize() { symbols->initializeRandomly(); }

		SymbolLearningData(int symbolCount);

		void SaveToManaged(HwrDataModel::SymbolClasses^ managedSymbols);
		void LoadFromManaged(HwrDataModel::SymbolClasses^ managedSymbols);

		void MergeInLearningCache(SymbolLearningData^ other);
		void AssertConsistency(System::String^ message);
	};
}