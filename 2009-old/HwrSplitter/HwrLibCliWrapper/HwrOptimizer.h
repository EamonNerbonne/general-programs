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
		HwrOptimizer(array<HwrDataModel::SymbolClass^>^ symbolClasses) : symbols(new AllSymbolClasses(symbolClasses->Length )) {
			symbols->initRandom();
		}

		array<int>^ SplitWords(ImageStruct<signed char> block, array<unsigned> ^ sequenceToMatch, [Out] int % topOffRef,float shear);
	};
}

