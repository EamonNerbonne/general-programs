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
		AllSymbolClasses* symbols;
		HwrDataModel::SymbolClasses^ managedSymbols;
		!HwrOptimizer() {
			if(symbols != NULL) {
				GC::RemoveMemoryPressure(symbols->AllocatedSize());
				delete symbols;
				symbols = NULL;
			}
		}
		~HwrOptimizer() { this->!HwrOptimizer(); }
	public:
		property int SymbolCount {int get() {return managedSymbols->Count;}}
		static void CopyToNative(HwrDataModel::SymbolClass ^ managedSymbol, SymbolClass & nativeSymbol);

		static void CopyToManaged(SymbolClass const & nativeSymbol, HwrDataModel::SymbolClass^ managedSymbol);

		HwrOptimizer(HwrDataModel::SymbolClasses^ symbolClasses) : symbols(new AllSymbolClasses(symbolClasses->Count )) {
			GC::AddMemoryPressure(symbols->AllocatedSize());
			managedSymbols = symbolClasses;
			
			symbols->initializeRandomly(); //0 variances are not permitted

			for(int i=0;i<symbolClasses->Count;i++) {
				if(symbolClasses[i]->Code != i)
					throw gcnew ArgumentException("Symbol position does not match its code");
				CopyToNative(symbolClasses[i] ,symbols->getSymbol(i));
			}
		}

		void SaveToManaged() {
			for(int i=0;i<managedSymbols->Count;i++) {
				if(managedSymbols[i]->Code != i)
					throw gcnew ArgumentException("Symbol position does not match its code");
				CopyToManaged(symbols->getSymbol(i), managedSymbols[i]);
			}
		}

		//array<double>^ GetFeatureVariances() {
		//	FeatureDistribution overall;
		//	for(int i=0;i<symbols->size();i++) {
		//		for(int j=0;j<SUB_SYMBOL_COUNT;j++) {
		//			overall.CombineWith(symbols->getSymbol(i).state[j]);
		//		}
		//	}

		//	array<double>^ retval = gcnew array<double>(NUMBER_OF_FEATURES);
		//	for(int i=0;i<NUMBER_OF_FEATURES;i++) 
		//		retval[i] = overall.varX(i);
		//	return retval;
		//}

		// block - the bit of the original (b/w) image, cropped to fit the line closely.
		// shear - the angle of text shearing in the input
		// sequenceToMatch - the sequence of symbolClass codes to match
		// overrideEnds - a manually specified endpoint (relative to block's start) for each symbol, negative for those (common) symbols where no manually specified endpoint exists
		// topOffRef - will be set to the amount of pixels the top row was shifted to account for shear
		// learningIteration - the current learning iteration; used to decrease the weight of symbolclasses (for instance) and to improve 
		void HwrOptimizer::SplitWords(ImageStruct<signed char> block, int cropXoffset, HwrDataModel::TextLine^ textLine, SymbolLearningData ^ dataSink);
	};
}

