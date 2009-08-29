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
			if(symbols != NULL) {
				GC::RemoveMemoryPressure(symbols->AllocatedSize());
				delete symbols;
				symbols = NULL;
			}
		}
		~HwrOptimizer() { this->!HwrOptimizer(); }
	public:
		static void CopyToNative(HwrDataModel::SymbolClass ^ managedSymbol, SymbolClass & nativeSymbol);

		static void CopyToManaged(SymbolClass const & nativeSymbol, HwrDataModel::SymbolClass^ managedSymbol);

		HwrOptimizer(array<HwrDataModel::SymbolClass^>^ symbolClasses) : symbols(new AllSymbolClasses(symbolClasses->Length )) {
			GC::AddMemoryPressure(symbols->AllocatedSize());
			symbols->initRandom();
			for(int i=0;i<symbolClasses->Length;i++) {
				if(symbolClasses[i]->Code != i)
					throw gcnew ArgumentException("Symbol position does not match its code");
				CopyToNative(symbolClasses[i],symbols->getSymbol(i));
			}
		}

		void SaveToManaged(array<HwrDataModel::SymbolClass^>^ symbolClasses) {
			for(int i=0;i<symbolClasses->Length;i++) {
				if(symbolClasses[i]->Code != i)
					throw gcnew ArgumentException("Symbol position does not match its code");
				CopyToManaged(symbols->getSymbol(i),symbolClasses[i]);
			}
		}

		// block - the bit of the original (b/w) image, cropped to fit the line closely.
		// sequenceToMatch - the sequence of symbolClass codes to match
		// topOffRef - will be set to the amount of pixels the top row was shifted to account for shear
		// shear - the angle of text shearing in the input
		// learningIteration - the current learning iteration; used to decrease the weight of symbolclasses (for instance) and to improve 
		array<int>^ SplitWords(ImageStruct<signed char> block, array<unsigned> ^ sequenceToMatch, [Out] int % topOffRef, float shear, int learningIteration);
	};
}

