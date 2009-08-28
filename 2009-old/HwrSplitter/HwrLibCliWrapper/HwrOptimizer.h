// HwrLibCliWrapper.h

#pragma once
#include "Stdafx.h"
#pragma warning (disable:4482)
#include "ImageStruct.h"
#include "ImageProcessor.h"

#include "WordSplitSolver.h"

namespace HwrLibCliWrapper {
	public ref class HwrOptimizer {
		AllSymbolClasses* symbols;
		!HwrOptimizer() {
			delete symbols;
			symbols = NULL;
		}
		~HwrOptimizer() { this->!HwrOptimizer(); }
	public:
		static void CopyToNative(HwrDataModel::SymbolClass ^ managedSymbol, SymbolClass & nativeSymbol);

		static void CopyToManaged(SymbolClass const & nativeSymbol, HwrDataModel::SymbolClass^ managedSymbol);

		HwrOptimizer(array<HwrDataModel::SymbolClass^>^ symbolClasses) : symbols(new AllSymbolClasses(symbolClasses->Length )) {
			symbols->initRandom();
			for(int i=0;i<symbolClasses->Length;i++) {
				if(symbolClasses[i]->Code != i)
					throw gcnew ArgumentException("Symbol position does not match its code");
				CopyToNative(symbolClasses[i],symbols->operator [](i));
			}
		}

		array<int>^ SplitWords(ImageStruct<signed char> block, array<unsigned> ^ sequenceToMatch, [Out] int % topOffRef,float shear);
	};
}

