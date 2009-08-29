// This is the main DLL file.

#include "stdafx.h"

#include "HwrOptimizer.h"

#include "image/transformations.h"

namespace HwrLibCliWrapper {

	void HwrOptimizer::CopyToNative(HwrDataModel::SymbolClass ^ managedSymbol, SymbolClass & nativeSymbol) {
		nativeSymbol.wLength = managedSymbol->Length->WeightSum;
		nativeSymbol.mLength = managedSymbol->Length->Mean;
		nativeSymbol.sLength = managedSymbol->Length->ScaledVariance;
		nativeSymbol.originalChar = managedSymbol->Letter;
		for(int i=0;i<SUB_SYMBOL_COUNT;i++) {
			if(managedSymbol->State !=nullptr 
				&& managedSymbol->State[i %  managedSymbol->State->Length] != nullptr 
				&& managedSymbol->State[i %  managedSymbol->State->Length]->means->Length == NUMBER_OF_FEATURES) {
				HwrDataModel::FeatureDistributionEstimate ^ mState = managedSymbol->State[i %  managedSymbol->State->Length];
				//we use modulo to reasonably support a changing number of substates: if native has more states, it just repeats other states.  If it has few; it truncates (implicitly).

				FeatureDistribution & state = nativeSymbol.state[i];
				
				state.weightSum = mState->weightSum;

				for(int j=0;j<NUMBER_OF_FEATURES;j++) {
					state.meanX[j] = mState->means[j];
					state.sX[j]=mState->scaledVars[j];
				}
				state.RecomputeDCfactor();
			}
		}
	}
	void HwrOptimizer::CopyToManaged(SymbolClass const & nativeSymbol, HwrDataModel::SymbolClass^ managedSymbol){
		if(managedSymbol->Letter != nativeSymbol.originalChar)
			throw gcnew ApplicationException("characters in C++ and C# out of sync");
		managedSymbol->Length = gcnew HwrDataModel::GaussianEstimate(nativeSymbol.mLength,nativeSymbol.sLength,nativeSymbol.wLength);
		managedSymbol->State = gcnew array<HwrDataModel::FeatureDistributionEstimate ^>(SUB_SYMBOL_COUNT);
		for(int i=0;i<SUB_SYMBOL_COUNT;i++) {
			managedSymbol->State[i] = gcnew HwrDataModel::FeatureDistributionEstimate();
			HwrDataModel::FeatureDistributionEstimate ^ mState = managedSymbol->State[i];
			FeatureDistribution const & state = nativeSymbol.state[i];

			mState->weightSum = state.weightSum;
			mState->means = gcnew array<double>(NUMBER_OF_FEATURES);
			mState->scaledVars = gcnew array<double>(NUMBER_OF_FEATURES);

			for(int j=0;j<NUMBER_OF_FEATURES;j++) {
				mState->means[j] = state.meanX[j];
				mState->scaledVars[j]=state.sX[j];
			}
		}
	}


	array<int>^ HwrOptimizer::SplitWords(ImageStruct<signed char> block, array<unsigned> ^ sequenceToMatch, [Out] int % topOffRef,float shear, int learningIteration) {
		using std::min;
		using std::max;
		using std::cout;
		using std::abs;
#if LOGLEVEL >=8
		boost::timer t;
#endif
		//based on learningIteration, set a few things:
		double dampingFactor = 1.0 - min(learningIteration/1000.0,1.0);
		int blurIter = 3;
		int winAngleSize = int(100.0*dampingFactor + 4);
		int winDensSize = int(winAngleSize*0.76);
		double featureRelevance = 0.1 * exp(-7*dampingFactor) ;


		PamImage<BWPixel> shearedImg = ImageProcessor::StructToPamImage(block);
		ImageBW unsheared = unshear(shearedImg,shear);
		topOffRef = shearedImg.getWidth() - unsheared.getWidth();
		ImageFeatures feats(unsheared,winDensSize,winAngleSize,blurIter);
		vector<short> sequenceVector;
		for(int i=0;i<sequenceToMatch->Length;i++) {
			unsigned tmp = sequenceToMatch[i];
			sequenceVector.push_back(tmp);
		}
#if LOGLEVEL >=8
		cout << "C++ textline prepare took " << t.elapsed() <<"\n";
#endif
		WordSplitSolver splitSolve( *symbols, feats, sequenceVector,featureRelevance);

		vector<int> splits = splitSolve.MostLikelySplit();
		array<int>^ retval = gcnew array<int>((int)splits.size());
		for(int i=0;i<(int)splits.size();i++) {
			retval[i] = splits[i];
		}

		splitSolve.Learn(dampingFactor);

		return retval;
	}
}